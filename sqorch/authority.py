import hashlib
import json
import re
import subprocess
import tomllib
from pathlib import Path


class AuthorityError(Exception):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


_TASK_BLOCK_START = re.compile(r"<!--\s*sqorch:task\s+v1\s*-->")
_TASK_BLOCK_END = re.compile(r"<!--\s*/sqorch:task\s*-->")
_TOML_FENCE_RE = re.compile(r"\A\s*```toml\n(.*?)```\s*\Z", re.DOTALL)
_ACTIVE_PACKET_RE = re.compile(r"^\s*-?\s*Active planning subplan:\s*`([^`]+)`\s*$", re.MULTILINE)
_ACTIVE_TASK_RE = re.compile(
    r"^\s*-?\s*Application implementation authorized:\s*\*\*yes\s+—\s+([^*]+?)\s+only\*\*\s*$",
    re.MULTILINE,
)
_WORKTREE_STATE_RE = re.compile(r"^\s*-?\s*Worktree state:\s*\*\*(clean|dirty)(?:\s+—[^*]*)?\*\*\s*$", re.MULTILINE)
_ALIAS_MODELS = frozenset({"latest", "auto", "default", "best", "strongest"})
_REQUIRED_FIELDS = frozenset(
    {
        "schema",
        "id",
        "role",
        "mode",
        "starting_commit",
        "allowed_paths",
        "forbidden_paths",
        "validation",
        "expected_commit_message",
        "external_call_limit",
        "spend_limit_usd",
        "turn_limit",
        "token_rotation_limit",
        "client",
        "model",
        "automatic_fallback",
        "evidence_destination",
        "acceptance_authority",
    }
)


def _canonical_json(value: object) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":")).encode("utf-8")


def extract_task_block(repo: Path, docs: Path, task_id: str) -> dict[str, object]:
    tasks_path = docs / "BUILD-TASKS.md"
    if not tasks_path.is_file():
        raise AuthorityError("AUTHORITY_MISSING", "BUILD-TASKS.md is missing")

    content = tasks_path.read_text(encoding="utf-8")
    blocks: list[dict[str, object]] = []
    position = 0
    while True:
        start = _TASK_BLOCK_START.search(content, position)
        if not start:
            break
        end = _TASK_BLOCK_END.search(content, start.end())
        if not end:
            raise AuthorityError("VALIDATION_FAILED", "Unterminated task block")
        match = _TOML_FENCE_RE.match(content[start.end() : end.start()])
        if not match:
            raise AuthorityError("VALIDATION_FAILED", "Task block must contain one TOML fence")
        try:
            block = tomllib.loads(match.group(1))
        except tomllib.TOMLDecodeError as error:
            raise AuthorityError("VALIDATION_FAILED", "Malformed TOML task block") from error
        blocks.append(block)
        position = end.end()

    if not blocks:
        raise AuthorityError("AUTHORITY_MISSING", "No task blocks found in BUILD-TASKS.md")

    matching = [block for block in blocks if block.get("id") == task_id]
    if len(matching) != 1:
        code = "AUTHORITY_MISSING" if not matching else "VALIDATION_FAILED"
        raise AuthorityError(code, f"Task {task_id} is missing or duplicated")
    return matching[0]


def _validate_relative_posix_path(path: object) -> str:
    if not isinstance(path, str) or not path:
        raise AuthorityError("VALIDATION_FAILED", "Task paths must be non-empty strings")
    if "\\" in path or path.startswith("/") or re.match(r"^[A-Za-z]:", path):
        raise AuthorityError("VALIDATION_FAILED", f"Task path is not relative POSIX: {path}")
    body = path[:-1] if path.endswith("/") else path
    if not body or "//" in body or any(part in {"", ".", ".."} for part in body.split("/")):
        raise AuthorityError("VALIDATION_FAILED", f"Invalid task path: {path}")
    return path


def _claims_overlap(left: str, right: str) -> bool:
    left_base = left.rstrip("/")
    right_base = right.rstrip("/")
    return (
        left_base == right_base
        or left.endswith("/") and right_base.startswith(f"{left_base}/")
        or right.endswith("/") and left_base.startswith(f"{right_base}/")
    )


def validate_paths(allowed: list[str], forbidden: list[str]) -> None:
    if not allowed or not forbidden:
        raise AuthorityError("VALIDATION_FAILED", "Allowed and forbidden paths are required")
    normalized_allowed = [_validate_relative_posix_path(path) for path in allowed]
    normalized_forbidden = [_validate_relative_posix_path(path) for path in forbidden]
    if len(set(normalized_allowed)) != len(normalized_allowed) or len(set(normalized_forbidden)) != len(normalized_forbidden):
        raise AuthorityError("VALIDATION_FAILED", "Duplicate task path claim")
    for allowed_path in normalized_allowed:
        for forbidden_path in normalized_forbidden:
            if _claims_overlap(allowed_path, forbidden_path):
                raise AuthorityError(
                    "VALIDATION_FAILED",
                    f"Allowed path '{allowed_path}' overlaps forbidden path '{forbidden_path}'",
                )


def validate_route(client: str, model: str, automatic_fallback: bool) -> None:
    if not isinstance(client, str) or not client:
        raise AuthorityError("ROUTE_INVALID", "Client must not be empty")
    if not isinstance(model, str) or not model or model.lower() in _ALIAS_MODELS:
        raise AuthorityError("ROUTE_INVALID", "Model must be an exact non-alias ID")
    if automatic_fallback is not False:
        raise AuthorityError("ROUTE_INVALID", "Automatic fallback must be disabled")


def compute_document_hashes(repo: Path, docs: Path) -> dict[str, str]:
    return {
        name: hashlib.sha256(path.read_bytes()).hexdigest()
        for name, path in (
            ("STATUS.md", repo / "STATUS.md"),
            ("PACKET.md", docs / "PACKET.md"),
            ("BUILD.md", docs / "BUILD.md"),
            ("BUILD-TASKS.md", docs / "BUILD-TASKS.md"),
        )
    }


def _status_match(pattern: re.Pattern[str], content: str, label: str) -> str:
    matches = pattern.findall(content)
    if len(matches) != 1:
        raise AuthorityError("AUTHORITY_MISSING", f"Exactly one {label} field is required")
    return matches[0]


def _find_docs_dir(repo: Path, status_content: str) -> Path:
    relative = _status_match(_ACTIVE_PACKET_RE, status_content, "active planning subplan")
    _validate_relative_posix_path(relative)
    docs = (repo / relative).resolve(strict=True)
    if docs != repo and repo not in docs.parents:
        raise AuthorityError("AUTHORITY_DRIFT", "Active packet is outside the repository")
    return docs


def _git_output(repo: Path, *args: str) -> str:
    try:
        result = subprocess.run(
            ["git", *args], cwd=repo, capture_output=True, text=True, check=True
        )
    except (FileNotFoundError, subprocess.CalledProcessError) as error:
        raise AuthorityError("AUTHORITY_DRIFT", "Git repository inspection failed") from error
    return result.stdout


def _validate_context_pairs(repo: Path) -> None:
    ignored_names = {".git", "__pycache__"}
    for directory in (repo, *sorted((path for path in repo.rglob("*") if path.is_dir()), key=str)):
        if any(part in ignored_names or part.startswith(".") for part in directory.relative_to(repo).parts):
            continue
        agents = directory / "AGENTS.md"
        claude = directory / "CLAUDE.md"
        if not agents.is_file() or not claude.is_file():
            raise AuthorityError("AUTHORITY_DRIFT", f"Context pair is incomplete at {directory}")


def _validate_task_schema(block: dict[str, object], task_id: str) -> None:
    if set(block) != _REQUIRED_FIELDS:
        raise AuthorityError("VALIDATION_FAILED", "Task block fields are incomplete or unknown")
    if block["schema"] != 1 or isinstance(block["schema"], bool):
        raise AuthorityError("VALIDATION_FAILED", "Task schema must be 1")
    if block["id"] != task_id:
        raise AuthorityError("AUTHORITY_DRIFT", "Requested task does not match task block")
    for field in ("role", "expected_commit_message", "client", "model", "evidence_destination", "acceptance_authority"):
        if not isinstance(block[field], str) or not block[field]:
            raise AuthorityError("VALIDATION_FAILED", f"Task block has invalid {field}")
    _validate_relative_posix_path(block["evidence_destination"])
    if block["mode"] not in {"read", "write"}:
        raise AuthorityError("VALIDATION_FAILED", "Task mode must be read or write")
    if not isinstance(block["starting_commit"], str) or not re.fullmatch(r"[0-9a-f]{40}", block["starting_commit"]):
        raise AuthorityError("VALIDATION_FAILED", "Task starting_commit must be a 40-character SHA")
    for field in ("allowed_paths", "forbidden_paths", "validation"):
        if not isinstance(block[field], list) or not block[field] or not all(isinstance(value, str) and value for value in block[field]):
            raise AuthorityError("VALIDATION_FAILED", f"Task block has invalid {field}")
    if not isinstance(block["automatic_fallback"], bool):
        raise AuthorityError("VALIDATION_FAILED", "automatic_fallback must be boolean")
    for field in ("external_call_limit", "turn_limit", "token_rotation_limit"):
        minimum = 0 if field == "external_call_limit" else 1
        if not isinstance(block[field], int) or isinstance(block[field], bool) or block[field] < minimum:
            raise AuthorityError("VALIDATION_FAILED", f"Task block has invalid {field}")
    if not isinstance(block["spend_limit_usd"], (int, float)) or isinstance(block["spend_limit_usd"], bool) or block["spend_limit_usd"] < 0:
        raise AuthorityError("VALIDATION_FAILED", "Task block has invalid spend_limit_usd")


def compile_manifest(repo: Path, task_id: str) -> bytes:
    repo = repo.resolve(strict=True)
    status_path = repo / "STATUS.md"
    if not status_path.is_file():
        raise AuthorityError("AUTHORITY_MISSING", "STATUS.md is missing")
    status_content = status_path.read_text(encoding="utf-8")
    docs = _find_docs_dir(repo, status_content)
    active_task = _status_match(_ACTIVE_TASK_RE, status_content, "implementation authorization")
    if active_task != task_id:
        raise AuthorityError("AUTHORITY_DRIFT", "Requested task is not the active authorized task")
    block = extract_task_block(repo, docs, task_id)
    _validate_task_schema(block, task_id)
    validate_paths(block["allowed_paths"], block["forbidden_paths"])
    validate_route(block["client"], block["model"], block["automatic_fallback"])
    if _git_output(repo, "rev-parse", "HEAD").strip() != block["starting_commit"]:
        raise AuthorityError("AUTHORITY_DRIFT", "HEAD does not match task starting_commit")
    declared_worktree = _status_match(_WORKTREE_STATE_RE, status_content, "worktree state")
    is_dirty = bool(_git_output(repo, "status", "--porcelain").strip())
    if (declared_worktree == "clean") == is_dirty:
        raise AuthorityError("AUTHORITY_DRIFT", "Worktree disclosure does not match Git status")
    _validate_context_pairs(repo)
    task = {field: value for field, value in block.items() if field != "schema"}
    return _canonical_json({"schema": 1, "task": task, "hashes": compute_document_hashes(repo, docs)})
