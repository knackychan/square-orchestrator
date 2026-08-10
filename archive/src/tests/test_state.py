import sqlite3
from pathlib import Path
import tempfile
import unittest


class StateRegistrationTests(unittest.TestCase):
    def test_schema_user_version_is_one(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db

            conn = init_db(db_path)
            try:
                version = conn.execute("PRAGMA user_version").fetchone()[0]
                self.assertEqual(version, 1)
            finally:
                conn.close()

    def test_project_table_columns_match_design(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db

            conn = init_db(db_path)
            try:
                project_columns = [
                    (row[1], row[2])
                    for row in conn.execute("PRAGMA table_info('projects')").fetchall()
                ]
                self.assertEqual(
                    project_columns,
                    [
                        ("canonical_path", "TEXT"),
                        ("display_name", "TEXT"),
                        ("policy_profile", "TEXT"),
                        ("added_at_utc", "TEXT"),
                    ],
                )
            finally:
                conn.close()

    def test_lock_table_acquired_at_is_text(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db

            conn = init_db(db_path)
            try:
                lock_columns = [
                    (row[1], row[2])
                    for row in conn.execute("PRAGMA table_info('locks')").fetchall()
                ]
                self.assertEqual(lock_columns[-1], ("acquired_at_utc", "TEXT"))
            finally:
                conn.close()

    def test_idempotent_registration_preserves_timestamp(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db, register_project
            from datetime import datetime, timezone

            timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn = init_db(db_path)
            try:
                first = register_project(
                    conn,
                    "C:/projects/test",
                    "Test Project",
                    "C:/profiles/default",
                    timestamp,
                )
                second = register_project(
                    conn,
                    "C:/projects/test",
                    "Test Project",
                    "C:/profiles/default",
                    timestamp,
                )
                self.assertEqual(first, second)
                self.assertEqual(first["added_at_utc"], second["added_at_utc"])
            finally:
                conn.close()

    def test_conflicting_registration_returns_state_conflict(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db, register_project
            from sqorch.application import ApplicationError
            from datetime import datetime, timezone

            timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn = init_db(db_path)
            try:
                register_project(
                    conn,
                    "C:/projects/test",
                    "Test Project",
                    "C:/profiles/default",
                    timestamp,
                )
                with self.assertRaises(ApplicationError) as context:
                    register_project(
                        conn,
                        "C:/projects/test",
                        "Different Name",
                        "C:/profiles/default",
                        timestamp,
                    )
                self.assertEqual(context.exception.code, "STATE_CONFLICT")
            finally:
                conn.close()


class StateLockTests(unittest.TestCase):
    def _register_and_init(self, db_path: Path) -> "tuple":
        from sqorch.state import init_db, register_project
        from datetime import datetime, timezone

        timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
            "%Y-%m-%dT%H:%M:%SZ"
        )
        conn = init_db(db_path)
        register_project(
            conn, "C:/projects/test", "Test Project", "C:/profiles/default", timestamp
        )
        return conn

    def test_holder_a_acquires_successfully(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import acquire_lock
            from datetime import datetime, timezone

            timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn = self._register_and_init(db_path)
            try:
                result = acquire_lock(
                    conn,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    timestamp,
                )
                self.assertTrue(result)
            finally:
                conn.close()

    def test_holder_b_locked_when_holder_a_owns(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.application import ApplicationError
            from sqorch.state import acquire_lock
            from datetime import datetime, timezone

            timestamp_a = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            timestamp_b = datetime(2025, 1, 2, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn_a = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn_a,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    timestamp_a,
                )
            except Exception:
                conn_a.close()
                raise

            conn_b = sqlite3.connect(str(db_path))
            try:
                with self.assertRaises(ApplicationError) as context:
                    acquire_lock(
                        conn_b,
                        "C:/projects/test",
                        "holder-b",
                        "def456",
                        timestamp_b,
                    )
                self.assertEqual(context.exception.code, "LOCKED")
            finally:
                conn_b.close()
                conn_a.close()

    def test_holder_b_cannot_release_holder_a_lock(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import acquire_lock, release_lock
            from datetime import datetime, timezone

            timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn_a = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn_a,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    timestamp,
                )
            except Exception:
                conn_a.close()
                raise

            conn_b = sqlite3.connect(str(db_path))
            try:
                released = release_lock(conn_b, "C:/projects/test", "holder-b")
                self.assertFalse(released)
            finally:
                conn_b.close()
                conn_a.close()

    def test_holder_a_releases_own_lock_successfully(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import acquire_lock, release_lock
            from datetime import datetime, timezone

            timestamp = datetime(2025, 1, 1, tzinfo=timezone.utc).strftime(
                "%Y-%m-%dT%H:%M:%SZ"
            )
            conn = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    timestamp,
                )
                released = release_lock(conn, "C:/projects/test", "holder-a")
                self.assertTrue(released)
            finally:
                conn.close()


if __name__ == "__main__":
    unittest.main()
