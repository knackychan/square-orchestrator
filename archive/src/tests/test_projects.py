import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from sqorch.application import ApplicationError, preview, audit
from tests.support import init_git_repo, tree_digest


CANONICAL_INPUT = {
    "product_boundary": "A CLI that coordinates bounded agent work.",
    "owner": "Owner",
    "language": "Python",
    "deployment_context": "Local terminal",
    "external_effects": "none",
    "data_sensitivity": "low",
    "expected_scale": "single repository",
    "acceptance_authority": "primary session",
    "responsibilities": [
        {"id": "cli", "description": "Argument parsing and rendering", "owned_path": "sqorch/cli.py"},
        {"id": "application", "description": "Use-case coordination", "owned_path": "sqorch/application.py"},
    ],
    "dependencies": [{"from": "cli", "to": "application"}],
}


class ProjectPreviewTests(unittest.TestCase):
    def test_dependency_order_is_acyclic(self) -> None:
        result = preview(CANONICAL_INPUT)
        self.assertEqual(
            result["dependency_order"],
            ["application", "cli"],
        )

    def test_cycle_returns_invalid_input(self) -> None:
        input_data = dict(CANONICAL_INPUT)
        input_data["dependencies"] = [
            {"from": "cli", "to": "application"},
            {"from": "application", "to": "cli"},
        ]
        with self.assertRaises(ApplicationError) as ctx:
            preview(input_data)
        self.assertEqual(ctx.exception.code, "INVALID_INPUT")

    def test_duplicate_owner_returns_invalid_input(self) -> None:
        input_data = dict(CANONICAL_INPUT)
        input_data["responsibilities"] = [
            {"id": "cli", "description": "Argument parsing", "owned_path": "sqorch/cli.py"},
            {"id": "other", "description": "Another responsibility", "owned_path": "sqorch/cli.py"},
        ]
        with self.assertRaises(ApplicationError) as ctx:
            preview(input_data)
        self.assertEqual(ctx.exception.code, "INVALID_INPUT")

    def test_preview_does_not_mutate_target_tree(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            target = Path(temporary_directory)
            before = tree_digest(target)
            preview(CANONICAL_INPUT)
            after = tree_digest(target)
            self.assertEqual(before, after)

    def test_preview_names_required_authority_files(self) -> None:
        result = preview(CANONICAL_INPUT)
        self.assertEqual(
            result["authority_files"],
            [
                "AGENTS.md",
                "CLAUDE.md",
                "SPEC.md",
                "STATUS.md",
                "HANDOVER.md",
            ],
        )
        self.assertIn("docs/superpowers/AGENTS.md", result["context_pairs"])
        self.assertIn("docs/superpowers/CLAUDE.md", result["context_pairs"])


class ProjectAuditTests(unittest.TestCase):
    def make_repository(self) -> Path:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        (tmp / "AGENTS.md").write_text("# Agents\n", encoding="utf-8")
        (tmp / "CLAUDE.md").write_text("# Claude\n", encoding="utf-8")
        (tmp / "SPEC.md").write_text("# Spec\n", encoding="utf-8")
        (tmp / "HANDOVER.md").write_text("# Handover\n", encoding="utf-8")
        (tmp / "README.md").write_text("# Readme\n", encoding="utf-8")
        (tmp / "src").mkdir()
        (tmp / "src" / "main.py").write_text("print('hi')\n", encoding="utf-8")
        (tmp / "tests").mkdir()
        (tmp / "tests" / "test_main.py").write_text("def test_main():\n    pass\n", encoding="utf-8")
        status = "# Status\n\nActive planning subplan: `docs/plans/example`\n"
        (tmp / "STATUS.md").write_text(status, encoding="utf-8")
        (tmp / "docs" / "plans" / "example").mkdir(parents=True)
        subprocess.run(["git", "add", "."], cwd=tmp, capture_output=True, check=True)
        subprocess.run(["git", "commit", "-m", "fixture"], cwd=tmp, capture_output=True, check=True)
        return tmp

    def test_audit_reports_inventory_without_mutation(self) -> None:
        tmp = self.make_repository()
        before = tree_digest(tmp)
        result = audit(str(tmp))
        after = tree_digest(tmp)
        self.assertEqual(before, after)
        self.assertEqual(result["head"], self._head(tmp))
        self.assertTrue(result["worktree_clean"])
        self.assertTrue(result["authority_files"]["AGENTS.md"])
        self.assertIn("tests", result["top_level"]["test"])
        self.assertTrue(result["active_packet_exists"])

    def test_audit_non_repository_returns_not_a_repository(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            target = Path(temporary_directory)
            (target / "placeholder.txt").write_text("x\n", encoding="utf-8")
            with self.assertRaises(ApplicationError) as ctx:
                audit(str(target))
            self.assertEqual(ctx.exception.code, "NOT_A_REPOSITORY")

    def _head(self, tmp: Path) -> str:
        import subprocess

        return subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=tmp, capture_output=True, text=True, check=True
        ).stdout.strip()


if __name__ == "__main__":
    unittest.main()
