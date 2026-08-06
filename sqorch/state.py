import sqlite3
from pathlib import Path


def _normalize(path: str) -> str:
    return str(Path(path).resolve())


def _app_error(code: str, message: str, exit_code: int) -> "Exception":
    from .application import ApplicationError

    return ApplicationError(code, message, exit_code=exit_code)


def init_db(db_path: str | Path) -> sqlite3.Connection:
    path = Path(db_path)
    path.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(str(path))
    conn.execute("PRAGMA foreign_keys = ON")
    conn.execute(
        "CREATE TABLE IF NOT EXISTS projects ("
        "  project_path TEXT PRIMARY KEY,"
        "  profile_path TEXT NOT NULL,"
        "  registered_at_utc REAL NOT NULL"
        ")"
    )
    conn.execute(
        "CREATE TABLE IF NOT EXISTS locks ("
        "  project_path TEXT PRIMARY KEY REFERENCES projects(project_path),"
        "  holder TEXT NOT NULL,"
        "  starting_commit TEXT NOT NULL,"
        "  acquired_at_utc REAL NOT NULL"
        ")"
    )
    conn.execute("PRAGMA user_version = 1")
    conn.commit()
    return conn


def register_project(
    conn: sqlite3.Connection,
    project_path: str,
    profile_path: str,
    registered_at_utc: float,
) -> dict[str, object]:
    normalized_project = _normalize(project_path)
    normalized_profile = _normalize(profile_path)
    existing = conn.execute(
        "SELECT project_path, profile_path, registered_at_utc FROM projects WHERE project_path = ?",
        (normalized_project,),
    ).fetchone()
    if existing is not None:
        if (
            existing[1] == normalized_profile
            and existing[2] == registered_at_utc
        ):
            return {
                "project_path": existing[0],
                "profile_path": existing[1],
                "registered_at_utc": existing[2],
            }
        raise _app_error(
            "STATE_CONFLICT",
            f"Project {normalized_project} is already registered with different values.",
            exit_code=4,
        )
    conn.execute(
        "INSERT INTO projects (project_path, profile_path, registered_at_utc) VALUES (?, ?, ?)",
        (normalized_project, normalized_profile, registered_at_utc),
    )
    conn.commit()
    return {
        "project_path": normalized_project,
        "profile_path": normalized_profile,
        "registered_at_utc": registered_at_utc,
    }


def acquire_lock(
    conn: sqlite3.Connection,
    project_path: str,
    holder: str,
    starting_commit: str,
    acquired_at_utc: float,
) -> bool:
    normalized_project = _normalize(project_path)
    try:
        conn.execute("BEGIN IMMEDIATE")
        conn.execute(
            "INSERT INTO locks (project_path, holder, starting_commit, acquired_at_utc) VALUES (?, ?, ?, ?)",
            (normalized_project, holder, starting_commit, acquired_at_utc),
        )
        conn.commit()
        return True
    except sqlite3.IntegrityError:
        conn.rollback()
        raise _app_error(
            "LOCKED",
            f"Project {normalized_project} is locked by another holder.",
            exit_code=4,
        )


def release_lock(
    conn: sqlite3.Connection,
    project_path: str,
    holder: str,
) -> bool:
    normalized_project = _normalize(project_path)
    cursor = conn.execute(
        "DELETE FROM locks WHERE project_path = ? AND holder = ?",
        (normalized_project, holder),
    )
    conn.commit()
    return cursor.rowcount > 0
