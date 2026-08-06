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

    def test_idempotent_registration_returns_same_record(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import init_db, register_project

            conn = init_db(db_path)
            try:
                project = {
                    "project_path": "C:/projects/test",
                    "profile_path": "C:/profiles/default",
                    "registered_at_utc": 1700000000.0,
                }
                first = register_project(
                    conn,
                    project["project_path"],
                    project["profile_path"],
                    project["registered_at_utc"],
                )
                second = register_project(
                    conn,
                    project["project_path"],
                    project["profile_path"],
                    project["registered_at_utc"],
                )
                self.assertEqual(first, second)
            finally:
                conn.close()

    def test_conflicting_registration_returns_state_conflict(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.application import ApplicationError
            from sqorch.state import init_db, register_project

            conn = init_db(db_path)
            try:
                register_project(
                    conn,
                    "C:/projects/test",
                    "C:/profiles/default",
                    1700000000.0,
                )
                with self.assertRaises(ApplicationError) as context:
                    register_project(
                        conn,
                        "C:/projects/test",
                        "C:/profiles/different",
                        1700000000.0,
                    )
                self.assertEqual(context.exception.code, "STATE_CONFLICT")
            finally:
                conn.close()


class StateLockTests(unittest.TestCase):
    def _register_and_init(self, db_path: Path) -> "tuple":
        from sqorch.state import init_db, register_project

        conn = init_db(db_path)
        register_project(conn, "C:/projects/test", "C:/profiles/default", 1700000000.0)
        return conn

    def test_holder_a_acquires_successfully(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import acquire_lock

            conn = self._register_and_init(db_path)
            try:
                result = acquire_lock(
                    conn,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    1700000000.0,
                )
                self.assertTrue(result)
            finally:
                conn.close()

    def test_holder_b_locked_when_holder_a_owns(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.application import ApplicationError
            from sqorch.state import acquire_lock

            conn_a = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn_a,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    1700000000.0,
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
                        1700000001.0,
                    )
                self.assertEqual(context.exception.code, "LOCKED")
            finally:
                conn_b.close()
                conn_a.close()

    def test_holder_b_cannot_release_holder_a_lock(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            db_path = Path(temporary_directory) / "state.db"
            from sqorch.state import acquire_lock, release_lock

            conn_a = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn_a,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    1700000000.0,
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

            conn = self._register_and_init(db_path)
            try:
                acquire_lock(
                    conn,
                    "C:/projects/test",
                    "holder-a",
                    "abc123",
                    1700000000.0,
                )
                released = release_lock(conn, "C:/projects/test", "holder-a")
                self.assertTrue(released)
            finally:
                conn.close()


if __name__ == "__main__":
    unittest.main()
