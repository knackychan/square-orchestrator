import json
import os
from pathlib import Path
import platform
import subprocess

from .authority import AuthorityError, compile_manifest
from .practices import PracticeError, validate as validate_practice_domain
from .projects import ProjectError, audit as audit_domain, preview as preview_domain


class ApplicationError(Exception):
    def __init__(self, code: str, message: str, *, exit_code: int) -> None:
        super().__init__(message)
        self.code = code
        self.message = message
        self.exit_code = exit_code


def doctor(state_db: str | None = None) -> dict[str, str]:
    repository = Path.cwd().resolve(strict=True)
    state_path = _state_path(state_db)
    try:
        git_version = subprocess.run(
            ["git", "--version"],
            cwd=repository,
            capture_output=True,
            text=True,
            check=True,
        ).stdout.strip()
    except (FileNotFoundError, subprocess.CalledProcessError) as error:
        raise ApplicationError("VALIDATION_FAILED", "Git is unavailable.", exit_code=3) from error

    return {
        "git": git_version,
        "python": platform.python_version(),
        "repository": str(repository),
        "state_db": str(state_path),
    }


def validate(project: str, task_id: str) -> dict[str, object]:
    try:
        manifest = compile_manifest(Path(project), task_id)
    except AuthorityError as error:
        raise ApplicationError(error.code, error.message, exit_code=3) from error
    return json.loads(manifest)


def preview_projects(blueprint: str) -> dict[str, object]:
    try:
        return preview_domain(blueprint)
    except ProjectError as error:
        raise ApplicationError(error.code, error.message, exit_code=2) from error


def audit_projects(repository: str) -> dict[str, object]:
    try:
        return audit_domain(repository)
    except ProjectError as error:
        raise ApplicationError(error.code, error.message, exit_code=2) from error


def preview(blueprint: dict[str, object] | str | Path) -> dict[str, object]:
    try:
        return preview_domain(blueprint)
    except ProjectError as error:
        raise ApplicationError(error.code, error.message, exit_code=2) from error


def audit(repository: str | Path) -> dict[str, object]:
    try:
        return audit_domain(repository)
    except ProjectError as error:
        raise ApplicationError(error.code, error.message, exit_code=2) from error


def validate_practices(path: str | Path) -> dict[str, object]:
    input_path = Path(path)
    try:
        raw = input_path.read_text(encoding="utf-8")
    except OSError as error:
        raise ApplicationError("INVALID_INPUT", f"Cannot read practice file: {error}", exit_code=2) from error
    try:
        record = json.loads(raw)
    except json.JSONDecodeError as error:
        raise ApplicationError("INVALID_INPUT", f"Invalid JSON: {error}", exit_code=2) from error
    if not isinstance(record, dict):
        raise ApplicationError("INVALID_INPUT", "Practice record must be a JSON object", exit_code=2)
    result = validate_practice_domain(record)
    if result.error is not None:
        raise ApplicationError(result.error.code, result.error.message, exit_code=2)
    return record


def _state_path(override: str | None) -> Path:
    if override:
        return Path(override).expanduser().resolve()
    local_app_data = os.environ.get("LOCALAPPDATA")
    if not local_app_data:
        raise ApplicationError(
            "INVALID_INPUT",
            "LOCALAPPDATA is required when --state-db is not supplied.",
            exit_code=2,
        )
    return (Path(local_app_data) / "SquareOrchestrator" / "state.db").resolve()
