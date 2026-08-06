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
        "  canonical_path TEXT PRIMARY KEY,"
        "  display_name TEXT NOT NULL,"
        "  policy_profile TEXT NOT NULL,"
        "  added_at_utc TEXT NOT NULL"
        ")"
    )
    conn.execute(
        "CREATE TABLE IF NOT EXISTS locks ("
        "  project_path TEXT PRIMARY KEY REFERENCES projects(canonical_path),"
        "  holder TEXT NOT NULL,"
        "  starting_commit TEXT NOT NULL,"
        "  acquired_at_utc TEXT NOT NULL"
        ")"
    )
    conn.execute("PRAGMA user_version = 1")
    conn.commit()
    return conn


def register_project(
    conn: sqlite3.Connection,
    project_path: str,
    display_name: str,
    policy_profile: str,
    added_at_utc: str,
) -> dict[str, object]:
    normalized_project = _normalize(project_path)
    normalized_profile = _normalize(policy_profile)
    existing = conn.execute(
        "SELECT canonical_path, display_name, policy_profile, added_at_utc FROM projects WHERE canonical_path = ?",
        (normalized_project,),
    ).fetchone()
    if existing is not None:
        if (
            existing[1] == display_name
            and existing[2] == normalized_profile
        ):
            return {
                "canonical_path": existing[0],
                "display_name": existing[1],
                "policy_profile": existing[2],
                "added_at_utc": existing[3],
            }
        raise _app_error(
            "STATE_CONFLICT",
            f"Project {normalized_project} is already registered with different values.",
            exit_code=4,
        )
    conn.execute(
        "INSERT INTO projects (canonical_path, display_name, policy_profile, added_at_utc) VALUES (?, ?, ?, ?)",
        (normalized_project, display_name, normalized_profile, added_at_utc),
    )
    conn.commit()
    return {
        "canonical_path": normalized_project,
        "display_name": display_name,
        "policy_profile": normalized_profile,
        "added_at_utc": added_at_utc,
    }


def lookup_project(
    conn: sqlite3.Connection,
    project_path: str,
) -> dict[str, object] | None:
    normalized = _normalize(project_path)
    row = conn.execute(
        "SELECT canonical_path, display_name, policy_profile, added_at_utc FROM projects WHERE canonical_path = ?",
        (normalized,),
    ).fetchone()
    if row is None:
        return None
    return {
        "canonical_path": row[0],
        "display_name": row[1],
        "policy_profile": row[2],
        "added_at_utc": row[3],
    }


def acquire_lock(
    conn: sqlite3.Connection,
    project_path: str,
    holder: str,
    starting_commit: str,
    acquired_at_utc: str,
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
