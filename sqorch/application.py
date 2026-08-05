import os
from pathlib import Path
import platform
import subprocess


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
