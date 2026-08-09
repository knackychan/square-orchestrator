import json
import os
import platform
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]


class DoctorCliTests(unittest.TestCase):
    def run_cli(self, *args: str, local_app_data: Path) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        environment["LOCALAPPDATA"] = str(local_app_data)
        environment["PYTHONPATH"] = str(ROOT)
        return subprocess.run(
            [sys.executable, "-m", "sqorch", *args],
            cwd=ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_doctor_json_reports_environment_without_creating_state(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            completed = self.run_cli("--json", "doctor", local_app_data=local_app_data)

            self.assertEqual(completed.returncode, 0, completed.stderr)
            json_result = json.loads(completed.stdout)
            expected_git_version = subprocess.run(
                ["git", "--version"],
                cwd=ROOT,
                capture_output=True,
                text=True,
                check=True,
            ).stdout.strip()
            expected_python_version = platform.python_version()
            expected_repository = str(ROOT.resolve())
            expected_state_path = str(state_path.resolve())

            assert json_result == {
                "ok": True,
                "data": {
                    "git": expected_git_version,
                    "python": expected_python_version,
                    "repository": expected_repository,
                    "state_db": expected_state_path,
                },
            }
            assert not state_path.exists()

    def test_invalid_command_returns_two(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            invalid_command = self.run_cli(
                "--json", "not-a-command", local_app_data=Path(temporary_directory)
            )

            assert invalid_command.returncode == 2

    def test_validate_json_returns_compiled_manifest(self) -> None:
        from tests.support import make_authority_fixture

        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture = make_authority_fixture(Path(temporary_directory) / "fixture")
            completed = self.run_cli(
                "--json",
                "validate",
                "--project",
                str(fixture),
                "--task",
                "T-TEST-01",
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertTrue(result["ok"])
            self.assertEqual(result["data"]["task"]["id"], "T-TEST-01")

    def test_project_new_preview_requires_preview_flag(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            input_path = Path(temporary_directory) / "blueprint.json"
            input_path.write_text("{}", encoding="utf-8")
            completed = self.run_cli(
                "--json",
                "project",
                "new",
                "--input",
                str(input_path),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 2, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(result["error"]["code"], "INVALID_INPUT")

    def test_project_adopt_requires_audit_only_flag(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            target = Path(temporary_directory) / "repo"
            target.mkdir()
            completed = self.run_cli(
                "--json",
                "project",
                "adopt",
                str(target),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 2, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(result["error"]["code"], "INVALID_INPUT")

    def test_practices_validate_json_success(self) -> None:
        from tests.support import tree_digest

        repository = ROOT
        unchanged_digest = tree_digest(repository)

        with tempfile.TemporaryDirectory() as temporary_directory:
            input_path = Path(temporary_directory) / "practice.json"
            canonical_record = {
                "schema": "practice/v1",
                "id": "P-001",
                "category": "testing",
                "statement": "Always write tests first",
                "proposed_scope": "project",
                "source_type": "observation",
                "provenance_reference": "T-M1-04 review",
                "observed_context": "Test project M1 dry run",
                "trade_offs": ["slower initial velocity"],
                "counterexamples": [],
                "confidence": 0.9,
                "review_date": "2026-08-06",
                "state": "CANDIDATE",
                "approving_authority": None,
                "affected_profiles": [],
            }
            input_path.write_text(json.dumps(canonical_record), encoding="utf-8")

            completed = self.run_cli(
                "--json",
                "practices",
                "validate",
                str(input_path),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(
                result,
                {"ok": True, "data": canonical_record},
            )

            assert tree_digest(repository) == unchanged_digest

    def test_practices_validate_human_output(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            input_path = Path(temporary_directory) / "practice.json"
            canonical_record = {
                "schema": "practice/v1",
                "id": "P-001",
                "category": "testing",
                "statement": "Always write tests first",
                "proposed_scope": "project",
                "source_type": "observation",
                "provenance_reference": "T-M1-04 review",
                "observed_context": "Test project M1 dry run",
                "trade_offs": ["slower initial velocity"],
                "counterexamples": [],
                "confidence": 0.9,
                "review_date": "2026-08-06",
                "state": "CANDIDATE",
                "approving_authority": None,
                "affected_profiles": [],
            }
            input_path.write_text(json.dumps(canonical_record), encoding="utf-8")

            completed = self.run_cli(
                "practices",
                "validate",
                str(input_path),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            self.assertEqual(
                json.loads(completed.stdout),
                canonical_record,
            )

    def test_practices_validate_malformed_json(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            input_path = Path(temporary_directory) / "bad.json"
            input_path.write_text("not json", encoding="utf-8")

            completed = self.run_cli(
                "--json",
                "practices",
                "validate",
                str(input_path),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 2, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(result["error"]["code"], "INVALID_INPUT")

    def test_practices_validate_invalid_utf8(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            input_path = Path(temporary_directory) / "bad_encoding.json"
            input_path.write_bytes(b"\xff")

            completed = self.run_cli(
                "--json",
                "practices",
                "validate",
                str(input_path),
                local_app_data=Path(temporary_directory),
            )

            self.assertEqual(completed.returncode, 2, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(result["error"]["code"], "INVALID_INPUT")
            self.assertEqual(completed.stderr, "")

    def test_project_add_registers_with_name_and_profile(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            completed = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(ROOT),
                "--name",
                "Test Project",
                "--profile",
                str(Path(temporary_directory) / "profile.json"),
                local_app_data=local_app_data,
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertTrue(result["ok"])
            self.assertEqual(result["data"]["canonical_path"], str(ROOT.resolve()))
            self.assertEqual(result["data"]["display_name"], "Test Project")

    def test_idempotent_project_add_returns_same_timestamp(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            profile_path = Path(temporary_directory) / "profile.json"
            first = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(ROOT),
                "--name",
                "Test Project",
                "--profile",
                str(profile_path),
                local_app_data=local_app_data,
            )
            second = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(ROOT),
                "--name",
                "Test Project",
                "--profile",
                str(profile_path),
                local_app_data=local_app_data,
            )

            self.assertEqual(first.returncode, 0, first.stderr)
            self.assertEqual(second.returncode, 0, second.stderr)
            first_data = json.loads(first.stdout)
            second_data = json.loads(second.stdout)
            self.assertEqual(first_data, second_data)
            self.assertEqual(
                first_data["data"]["added_at_utc"],
                second_data["data"]["added_at_utc"],
            )

    def test_project_add_changed_name_returns_state_conflict(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            profile_path = Path(temporary_directory) / "profile.json"
            self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(ROOT),
                "--name",
                "First Name",
                "--profile",
                str(profile_path),
                local_app_data=local_app_data,
            )

            changed = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(ROOT),
                "--name",
                "Different Name",
                "--profile",
                str(profile_path),
                local_app_data=local_app_data,
            )

            self.assertEqual(changed.returncode, 4, changed.stderr)
            result = json.loads(changed.stdout)
            self.assertEqual(result["error"]["code"], "STATE_CONFLICT")

    def test_run_dry_run_with_authority_fixture(self) -> None:
        from tests.support import make_authority_fixture

        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture = make_authority_fixture(Path(temporary_directory) / "fixture")
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            completed = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "run",
                "--project",
                str(fixture),
                "--task",
                "T-TEST-01",
                "--dry-run",
                local_app_data=local_app_data,
            )

            self.assertEqual(completed.returncode, 0, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertTrue(result["ok"])
            self.assertIs(result["data"]["launch_performed"], False)
            self.assertIs(result["data"]["automatic_fallback"], False)

    def test_run_dry_run_authority_drift_returns_exit_three(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            completed = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "run",
                "--project",
                str(ROOT),
                "--task",
                "T-M1-05",
                "--dry-run",
                local_app_data=local_app_data,
            )

            self.assertEqual(completed.returncode, 3, completed.stderr)
            result = json.loads(completed.stdout)
            self.assertEqual(result["error"]["code"], "AUTHORITY_DRIFT")
            self.assertEqual(completed.stderr, "")

    def test_registered_project_located_during_dry_run(self) -> None:
        from tests.support import make_authority_fixture
        import sqlite3 as _sqlite3

        with tempfile.TemporaryDirectory() as temporary_directory:
            local_app_data = Path(temporary_directory)
            state_path = local_app_data / "SquareOrchestrator" / "state.db"
            profile_path = Path(temporary_directory) / "profile.json"
            profile_path.write_text("{}", encoding="utf-8")

            fixture = make_authority_fixture(Path(temporary_directory) / "fixture")

            add_result = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "project",
                "add",
                str(fixture),
                "--name",
                "Registered Fixture",
                "--profile",
                str(profile_path),
                local_app_data=local_app_data,
            )
            self.assertEqual(add_result.returncode, 0, add_result.stderr)
            add_data = json.loads(add_result.stdout)
            stored_before = add_data["data"]

            dry_run = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "run",
                "--project",
                str(fixture),
                "--task",
                "T-TEST-01",
                "--dry-run",
                local_app_data=local_app_data,
            )
            self.assertEqual(dry_run.returncode, 0, dry_run.stderr)
            dry_run_data = json.loads(dry_run.stdout)
            self.assertIs(dry_run_data["data"]["launch_performed"], False)
            self.assertIs(dry_run_data["data"]["automatic_fallback"], False)

            conn = _sqlite3.connect(str(state_path))
            try:
                cursor = conn.execute(
                    "SELECT canonical_path, display_name, policy_profile, added_at_utc FROM projects WHERE canonical_path = ?",
                    (stored_before["canonical_path"],),
                )
                row = cursor.fetchone()
                self.assertIsNotNone(row)
                self.assertEqual(row[0], stored_before["canonical_path"])
                self.assertEqual(row[1], stored_before["display_name"])
                self.assertEqual(row[2], stored_before["policy_profile"])
                self.assertEqual(row[3], stored_before["added_at_utc"])

                lock_count = conn.execute("SELECT COUNT(*) FROM locks").fetchone()[0]
                self.assertEqual(lock_count, 0)
            finally:
                conn.close()

            fresh_fixture = make_authority_fixture(
                Path(temporary_directory) / "fixture2"
            )
            fresh_state = local_app_data / "SquareOrchestrator" / "state2.db"
            fresh_dry_run = self.run_cli(
                "--json",
                "--state-db",
                str(fresh_state),
                "run",
                "--project",
                str(fresh_fixture),
                "--task",
                "T-TEST-01",
                "--dry-run",
                local_app_data=local_app_data,
            )
            self.assertEqual(fresh_dry_run.returncode, 0, fresh_dry_run.stderr)

            drift = self.run_cli(
                "--json",
                "--state-db",
                str(state_path),
                "run",
                "--project",
                str(ROOT),
                "--task",
                "T-M1-05",
                "--dry-run",
                local_app_data=local_app_data,
            )
            self.assertEqual(drift.returncode, 3, drift.stderr)
            drift_result = json.loads(drift.stdout)
            self.assertEqual(drift_result["error"]["code"], "AUTHORITY_DRIFT")
            self.assertEqual(drift.stderr, "")


if __name__ == "__main__":
    unittest.main()
