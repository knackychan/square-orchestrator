"""Project foundry: responsibility-graph preview and read-only repository audit."""

import json
from pathlib import Path
import subprocess

_ROOT_AUTHORITY_FILES = ("AGENTS.md", "CLAUDE.md", "SPEC.md", "STATUS.md", "HANDOVER.md")
_FIRST_SLICE_FILES = (
    "AGENTS.md",
    "CLAUDE.md",
    "SPEC.md",
    "STATUS.md",
    "HANDOVER.md",
    "docs/superpowers/AGENTS.md",
    "docs/superpowers/CLAUDE.md",
    "docs/superpowers/specs/AGENTS.md",
    "docs/superpowers/specs/CLAUDE.md",
    "docs/superpowers/plans/AGENTS.md",
    "docs/superpowers/plans/CLAUDE.md",
)
_REQUIRED_BLUEPRINT_FIELDS = frozenset(
    {
        "product_boundary",
        "owner",
        "language",
        "deployment_context",
        "external_effects",
        "data_sensitivity",
        "expected_scale",
        "acceptance_authority",
        "responsibilities",
        "dependencies",
    }
)
_STATE_VOCABULARY = frozenset(
    {"OBSERVED", "CANDIDATE", "TRIAL", "ADOPTED", "REJECTED", "DEPRECATED"}
)


class ProjectError(Exception):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


def _canonical_path(path: Path) -> Path:
    return path.resolve(strict=True)


def _validate_relative_path(path: str, label: str) -> str:
    if not isinstance(path, str) or not path:
        raise ProjectError("INVALID_INPUT", f"{label} must be a non-empty relative path")
    if "\\" in path or path.startswith("/") or ":" in path:
        raise ProjectError("INVALID_INPUT", f"{label} must be a relative POSIX path")
    body = path[:-1] if path.endswith("/") else path
    if not body or "//" in body or any(part in {"", ".", ".."} for part in body.split("/")):
        raise ProjectError("INVALID_INPUT", f"{label} has invalid path segments")
    return path


def _validate_responsibilities(responsibilities: object) -> list[dict[str, str]]:
    if not isinstance(responsibilities, list) or not responsibilities:
        raise ProjectError("INVALID_INPUT", "responsibilities must be a non-empty list")
    seen_ids: set[str] = set()
    seen_paths: set[str] = set()
    for item in responsibilities:
        if not isinstance(item, dict):
            raise ProjectError("INVALID_INPUT", "responsibility must be an object")
        if set(item) != {"id", "description", "owned_path"}:
            raise ProjectError("INVALID_INPUT", "responsibility must have id, description, owned_path")
        identifier = item["id"]
        description = item["description"]
        if not isinstance(identifier, str) or not identifier:
            raise ProjectError("INVALID_INPUT", "responsibility id must be a non-empty string")
        if not isinstance(description, str) or not description:
            raise ProjectError("INVALID_INPUT", "responsibility description must be a non-empty string")
        owned_path = _validate_relative_path(item["owned_path"], "owned_path")
        if identifier in seen_ids:
            raise ProjectError("INVALID_INPUT", f"Duplicate responsibility id: {identifier}")
        if owned_path in seen_paths:
            raise ProjectError("INVALID_INPUT", f"Duplicate owned path: {owned_path}")
        seen_ids.add(identifier)
        seen_paths.add(owned_path)
    return responsibilities


def _validate_dependencies(dependencies: object, node_ids: set[str]) -> list[dict[str, str]]:
    if not isinstance(dependencies, list):
        raise ProjectError("INVALID_INPUT", "dependencies must be a list")
    for edge in dependencies:
        if not isinstance(edge, dict) or set(edge) != {"from", "to"}:
            raise ProjectError("INVALID_INPUT", "dependency edge must have from and to")
        source = edge["from"]
        target = edge["to"]
        if not isinstance(source, str) or not isinstance(target, str):
            raise ProjectError("INVALID_INPUT", "dependency endpoints must be strings")
        if source not in node_ids:
            raise ProjectError("INVALID_INPUT", f"Unknown dependency source: {source}")
        if target not in node_ids:
            raise ProjectError("INVALID_INPUT", f"Unknown dependency target: {target}")
    return dependencies


def _dependency_order(responsibilities: list[dict[str, str]], dependencies: list[dict[str, str]]) -> list[str]:
    node_ids = [item["id"] for item in responsibilities]
    edges = [(edge["from"], edge["to"]) for edge in dependencies]
    in_degree = {node: 0 for node in node_ids}
    adjacency = {node: [] for node in node_ids}
    for source, target in edges:
        adjacency[target].append(source)
        in_degree[source] += 1
    ready = [node for node in node_ids if in_degree[node] == 0]
    order: list[str] = []
    while ready:
        node = ready.pop(0)
        order.append(node)
        for target in adjacency[node]:
            in_degree[target] -= 1
            if in_degree[target] == 0:
                ready.append(target)
    if len(order) != len(node_ids):
        raise ProjectError("INVALID_INPUT", "Responsibility graph contains a cycle")
    return order


def preview(blueprint: dict[str, object] | str | Path) -> dict[str, object]:
    if isinstance(blueprint, (str, Path)):
        path = Path(blueprint).expanduser().resolve(strict=True)
        with path.open(encoding="utf-8") as handle:
            loaded: object = json.load(handle)
    else:
        loaded = blueprint
    if not isinstance(loaded, dict):
        raise ProjectError("INVALID_INPUT", "Blueprint input must be a JSON object")
    missing = sorted(_REQUIRED_BLUEPRINT_FIELDS - loaded.keys())
    if missing:
        raise ProjectError("INVALID_INPUT", f"Blueprint is missing fields: {', '.join(missing)}")
    unknown = sorted(loaded.keys() - _REQUIRED_BLUEPRINT_FIELDS)
    if unknown:
        raise ProjectError("INVALID_INPUT", f"Blueprint has unknown fields: {', '.join(unknown)}")
    responsibilities = _validate_responsibilities(loaded["responsibilities"])
    node_ids = {item["id"] for item in responsibilities}
    dependencies = _validate_dependencies(loaded["dependencies"], node_ids)
    order = _dependency_order(responsibilities, dependencies)
    return {
        "responsibilities": responsibilities,
        "dependencies": dependencies,
        "dependency_order": order,
        "authority_files": list(_ROOT_AUTHORITY_FILES),
        "context_pairs": [
            "AGENTS.md",
            "CLAUDE.md",
            "docs/superpowers/AGENTS.md",
            "docs/superpowers/CLAUDE.md",
            "docs/superpowers/specs/AGENTS.md",
            "docs/superpowers/specs/CLAUDE.md",
            "docs/superpowers/plans/AGENTS.md",
            "docs/superpowers/plans/CLAUDE.md",
        ],
        "first_slice_files": list(_FIRST_SLICE_FILES),
    }


def _status_marker(content: str, pattern: str) -> str | None:
    for line in content.splitlines():
        stripped = line.strip().lstrip("-").strip()
        if stripped.startswith(pattern):
            return stripped[len(pattern) :].strip()
    return None


def _context_pair_gaps(repo: Path) -> list[str]:
    gaps: list[str] = []
    for directory in sorted((path for path in repo.rglob("*") if path.is_dir() and ".git" not in path.parts), key=str):
        if not (directory / "AGENTS.md").is_file() or not (directory / "CLAUDE.md").is_file():
            gaps.append(directory.relative_to(repo).as_posix())
    return gaps


def _git_output(repo: Path, *args: str) -> str:
    try:
        result = subprocess.run(
            ["git", *args], cwd=repo, capture_output=True, text=True, check=True
        )
    except (FileNotFoundError, subprocess.CalledProcessError) as error:
        raise ProjectError("NOT_A_REPOSITORY", "Git repository inspection failed") from error
    return result.stdout


def _active_packet_path(repo: Path) -> str | None:
    status_path = repo / "STATUS.md"
    if not status_path.is_file():
        return None
    content = status_path.read_text(encoding="utf-8")
    marker = _status_marker(content, "Active planning subplan:")
    if not marker:
        return None
    return marker.strip("`").strip()


def audit(repository: str | Path) -> dict[str, object]:
    repo = _canonical_path(Path(repository))
    _git_output(repo, "rev-parse", "--is-inside-work-tree")
    head = _git_output(repo, "rev-parse", "HEAD").strip()
    porcelain = _git_output(repo, "status", "--porcelain").strip()
    worktree_clean = not porcelain
    authority_files = {name: (repo / name).is_file() for name in _ROOT_AUTHORITY_FILES}
    top_level: dict[str, list[str]] = {}
    for child in sorted((path for path in repo.iterdir() if path.name != ".git"), key=lambda path: path.name):
        if child.is_dir():
            if child.name == "tests" or child.name == "test":
                top_level.setdefault("test", []).append(child.name)
            else:
                top_level.setdefault("directory", []).append(child.name)
        elif child.suffix == ".py":
            top_level.setdefault("source", []).append(child.name)
        elif child.name in {"setup.py", "pyproject.toml", "package.json", "Cargo.toml", "go.mod"}:
            top_level.setdefault("package_metadata", []).append(child.name)
        elif child.name == "scripts":
            top_level.setdefault("scripts", []).append(child.name)
    active_packet = _active_packet_path(repo)
    active_packet_path = (repo / active_packet) if active_packet else None
    active_packet_exists = bool(active_packet_path and active_packet_path.is_dir())
    return {
        "head": head,
        "worktree_clean": worktree_clean,
        "authority_files": authority_files,
        "context_pair_gaps": _context_pair_gaps(repo),
        "top_level": top_level,
        "active_packet_exists": active_packet_exists,
    }
