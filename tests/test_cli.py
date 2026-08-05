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


if __name__ == "__main__":
    unittest.main()
