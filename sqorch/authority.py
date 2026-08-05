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
_TOML_FENCE_RE = re.compile(r"```toml\n(.*?)```", re.DOTALL)

_ALIAS_MODELS = frozenset({"latest", "auto", "default", "best", "strongest"})


def extract_task_block(repo: Path, docs: Path, task_id: str) -> dict[str, object]:
    tasks_path = docs / "BUILD-TASKS.md"
    if not tasks_path.exists():
        raise AuthorityError("AUTHORITY_MISSING", f"BUILD-TASKS.md not found at {tasks_path}")

    content = tasks_path.read_text(encoding="utf-8")
    blocks: list[dict[str, object]] = []
    pos = 0
    while True:
        start_match = _TASK_BLOCK_START.search(content, pos)
        if not start_match:
            break
        end_match = _TASK_BLOCK_END.search(content, start_match.end())
        if not end_match:
            break
        section = content[start_match.end() : end_match.start()]
        toml_match = _TOML_FENCE_RE.search(section)
        if toml_match:
            blocks.append(tomllib.loads(toml_match.group(1)))
        pos = end_match.end()

    if not blocks:
        raise AuthorityError("AUTHORITY_MISSING", "No task blocks found in BUILD-TASKS.md")

    matching = [b for b in blocks if b.get("id") == task_id]

    if not matching:
        raise AuthorityError("AUTHORITY_MISSING", f"Task {task_id} not found")

    if len(matching) > 1:
        raise AuthorityError("VALIDATION_FAILED", f"Duplicate task id: {task_id}")

    return matching[0]


def validate_paths(allowed: list[str], forbidden: list[str]) -> None:
    for path in allowed + forbidden:
        if not path:
            raise AuthorityError("VALIDATION_FAILED", "Empty path in task block")
        if path.startswith("/"):
            raise AuthorityError("VALIDATION_FAILED", f"Absolute path not allowed: {path}")
        parts = path.replace("\\", "/").rstrip("/").split("/")
        if ".." in parts or "." in parts:
            raise AuthorityError("VALIDATION_FAILED", f"Invalid path segment in: {path}")

    for a in allowed:
        for f in forbidden:
            a_dir = a.endswith("/")
            f_dir = f.endswith("/")
            if a_dir and f.startswith(a):
                raise AuthorityError(
                    "VALIDATION_FAILED",
                    f"Allowed path '{a}' overlaps forbidden path '{f}'",
                )
            if f_dir and a.startswith(f):
                raise AuthorityError(
                    "VALIDATION_FAILED",
                    f"Allowed path '{a}' overlaps forbidden path '{f}'",
                )
            if not a_dir and not f_dir and a == f:
                raise AuthorityError(
                    "VALIDATION_FAILED",
                    f"Allowed and forbidden paths both claim '{a}'",
                )


def validate_route(client: str, model: str, automatic_fallback: bool) -> None:
    if not client:
        raise AuthorityError("ROUTE_INVALID", "Client must not be empty")
    if not model:
        raise AuthorityError("ROUTE_INVALID", "Model must not be empty")
    if model.lower() in _ALIAS_MODELS:
        raise AuthorityError("ROUTE_INVALID", f"Alias model not allowed: {model}")
    if automatic_fallback is True:
        raise AuthorityError("ROUTE_INVALID", "Automatic fallback must be disabled")


def compute_document_hashes(repo: Path, docs: Path) -> dict[str, str]:
    hashes: dict[str, str] = {}
    for name, path in [
        ("STATUS.md", repo / "STATUS.md"),
        ("PACKET.md", docs / "PACKET.md"),
        ("BUILD.md", docs / "BUILD.md"),
        ("BUILD-TASKS.md", docs / "BUILD-TASKS.md"),
    ]:
        data = path.read_bytes()
        hashes[name] = hashlib.sha256(data).hexdigest()
    return hashes


def _find_docs_dir(repo: Path) -> Path:
    content = (repo / "STATUS.md").read_text(encoding="utf-8")
    match = re.search(r"Active planning subplan:\s*`([^`]+)`", content)
    if not match:
        raise AuthorityError("AUTHORITY_MISSING", "Active planning subplan not found in STATUS.md")
    return repo / match.group(1)


def _git_head(repo: Path) -> str:
    result = subprocess.run(
        ["git", "rev-parse", "HEAD"],
        cwd=repo,
        capture_output=True,
        text=True,
        check=True,
    )
    return result.stdout.strip()


def compile_manifest(repo: Path, task_id: str) -> dict[str, object]:
    docs = _find_docs_dir(repo)
    block = extract_task_block(repo, docs, task_id)

    schema = block.get("schema")
    allowed_paths_raw = block.get("allowed_paths")
    forbidden_paths_raw = block.get("forbidden_paths")
    starting_commit = block.get("starting_commit")
    client = block.get("client")
    model = block.get("model")
    automatic_fallback_raw = block.get("automatic_fallback")

    if starting_commit is None:
        raise AuthorityError("VALIDATION_FAILED", "Task block missing starting_commit")
    if client is None:
        raise AuthorityError("VALIDATION_FAILED", "Task block missing client")
    if model is None:
        raise AuthorityError("VALIDATION_FAILED", "Task block missing model")

    if isinstance(allowed_paths_raw, str):
        allowed_paths = json.loads(allowed_paths_raw)
    else:
        allowed_paths = list(allowed_paths_raw) if allowed_paths_raw else []

    if isinstance(forbidden_paths_raw, str):
        forbidden_paths = json.loads(forbidden_paths_raw)
    else:
        forbidden_paths = list(forbidden_paths_raw) if forbidden_paths_raw else []

    if isinstance(automatic_fallback_raw, str):
        auto_fallback = automatic_fallback_raw.lower() == "true"
    elif isinstance(automatic_fallback_raw, bool):
        auto_fallback = automatic_fallback_raw
    else:
        auto_fallback = bool(automatic_fallback_raw)

    actual_head = _git_head(repo)
    if actual_head != starting_commit:
        raise AuthorityError(
            "AUTHORITY_DRIFT",
            f"HEAD {actual_head} does not match task starting_commit {starting_commit}",
        )

    validate_paths(allowed_paths, forbidden_paths)
    validate_route(client, model, auto_fallback)

    hashes = compute_document_hashes(repo, docs)

    return {
        "schema": schema,
        "task": {
            "id": task_id,
            "role": block.get("role"),
            "mode": block.get("mode"),
            "starting_commit": starting_commit,
            "allowed_paths": allowed_paths,
            "forbidden_paths": forbidden_paths,
            "client": client,
            "model": model,
            "automatic_fallback": auto_fallback,
        },
        "hashes": hashes,
    }
