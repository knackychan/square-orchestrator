import json
import subprocess
import tempfile
import unittest
from pathlib import Path

from sqorch.authority import (
    AuthorityError,
    compile_manifest,
    compute_document_hashes,
    extract_task_block,
    validate_paths,
    validate_route,
)
from tests.support import (
    init_git_repo,
    toml_task_block,
    write_build_tasks,
    write_context_pairs,
    write_packet,
    write_status,
)


class AuthorityCompileTests(unittest.TestCase):
    def make_fixture(self) -> Path:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_context_pairs(tmp)
        write_context_pairs(docs)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(head)
        write_build_tasks(docs, block)
        return tmp

    def test_missing_block_returns_authority_missing(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        (docs / "BUILD-TASKS.md").write_text("# No blocks here\n", encoding="utf-8")

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "AUTHORITY_MISSING")

    def test_duplicate_task_id_returns_validation_failed(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(head)
        write_build_tasks(docs, block, block)

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_wrong_head_returns_authority_drift(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        wrong_head = "0" * 40
        block = toml_task_block(wrong_head)
        write_build_tasks(docs, block)

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "AUTHORITY_DRIFT")

    def test_overlapping_paths_returns_validation_failed(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(
            head,
            allowed_paths='["sqorch/"]',
            forbidden_paths='["sqorch/application.py"]',
        )
        write_build_tasks(docs, block)

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_alias_route_returns_route_invalid(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(head, model='"latest"')
        write_build_tasks(docs, block)

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "ROUTE_INVALID")

    def test_fallback_enabled_returns_route_invalid(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(head, automatic_fallback="true")
        write_build_tasks(docs, block)

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "ROUTE_INVALID")

    def test_compile_manifest_is_deterministic(self) -> None:
        tmp = self.make_fixture()
        first = compile_manifest(tmp, "T-TEST-01")
        second = compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(first, second)

    def test_document_hashes_are_exact(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        (docs / "BUILD-TASKS.md").write_bytes(b"")

        status_content = (tmp / "STATUS.md").read_bytes()
        packet_content = (docs / "PACKET.md").read_bytes()
        build_content = (docs / "BUILD.md").read_bytes()

        import hashlib

        expected_status = hashlib.sha256(status_content).hexdigest()
        expected_packet = hashlib.sha256(packet_content).hexdigest()
        expected_build = hashlib.sha256(build_content).hexdigest()

        hashes = compute_document_hashes(tmp, docs)
        self.assertEqual(hashes["STATUS.md"], expected_status)
        self.assertEqual(hashes["PACKET.md"], expected_packet)
        self.assertEqual(hashes["BUILD.md"], expected_build)
        self.assertIn("BUILD-TASKS.md", hashes)

    def test_manifest_is_canonical_json_bytes(self) -> None:
        tmp = self.make_fixture()
        manifest = compile_manifest(tmp, "T-TEST-01")
        expected = (
            b'{"hashes":{"BUILD-TASKS.md":"'
            + compute_document_hashes(
                tmp, tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
            )["BUILD-TASKS.md"].encode("ascii")
            + b'","BUILD.md":"'
            + compute_document_hashes(
                tmp, tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
            )["BUILD.md"].encode("ascii")
            + b'","PACKET.md":"'
            + compute_document_hashes(
                tmp, tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
            )["PACKET.md"].encode("ascii")
            + b'","STATUS.md":"'
            + compute_document_hashes(
                tmp, tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
            )["STATUS.md"].encode("ascii")
            + b'"},"schema":1,"task":{"acceptance_authority":"owner","allowed_paths":["sqorch/"],"automatic_fallback":false,"client":"cmdc","evidence_destination":"docs/STATE.md","expected_commit_message":"feat: test","external_call_limit":0,"forbidden_paths":["tests/"],"id":"T-TEST-01","mode":"write","model":"deepseek/deepseek-v4-pro","role":"IMPLEMENT","spend_limit_usd":0,"starting_commit":"'
            + subprocess.run(["git", "rev-parse", "HEAD"], cwd=tmp, capture_output=True, text=True, check=True).stdout.strip().encode("ascii")
            + b'","token_rotation_limit":150000,"turn_limit":100,"validation":["python -m unittest"]}}'
        )
        self.assertEqual(manifest, expected)
        self.assertEqual(json.loads(manifest), json.loads(expected))

    def test_rejects_inactive_requested_task(self) -> None:
        tmp = self.make_fixture()
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-OTHER")

        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "AUTHORITY_DRIFT")

    def test_rejects_unknown_field_and_missing_ceiling(self) -> None:
        for override in ({"unexpected": '"no"'}, {"turn_limit": None}):
            with self.subTest(override=override):
                tmp = self.make_fixture()
                docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
                block = toml_task_block(subprocess.run(["git", "rev-parse", "HEAD"], cwd=tmp, capture_output=True, text=True, check=True).stdout.strip(), **override)
                write_build_tasks(docs, block)

                with self.assertRaises(AuthorityError) as ctx:
                    compile_manifest(tmp, "T-TEST-01")
                self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_rejects_windows_and_empty_segment_paths(self) -> None:
        for claimed_path in ("a//b", "C:/absolute", "a\\b"):
            with self.subTest(claimed_path=claimed_path):
                with self.assertRaises(AuthorityError) as ctx:
                    validate_paths([claimed_path], [])
                self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_rejects_incorrect_worktree_disclosure_and_missing_context_pair(self) -> None:
        tmp = self.make_fixture()
        write_status(
            tmp,
            "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation",
            "T-TEST-01",
            worktree_state="clean",
        )
        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "AUTHORITY_DRIFT")

        tmp = self.make_fixture()
        (tmp / "CLAUDE.md").unlink()
        with self.assertRaises(AuthorityError) as ctx:
            compile_manifest(tmp, "T-TEST-01")
        self.assertEqual(ctx.exception.code, "AUTHORITY_DRIFT")

    def test_route_validation_rejects_empty_client(self) -> None:
        with self.assertRaises(AuthorityError) as ctx:
            validate_route("", "deepseek/deepseek-v4-pro", False)
        self.assertEqual(ctx.exception.code, "ROUTE_INVALID")

    def test_route_validation_rejects_empty_model(self) -> None:
        with self.assertRaises(AuthorityError) as ctx:
            validate_route("cmdc", "", False)
        self.assertEqual(ctx.exception.code, "ROUTE_INVALID")

    def test_path_validation_rejects_absolute(self) -> None:
        with self.assertRaises(AuthorityError) as ctx:
            validate_paths(["/absolute/path"], [])
        self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_path_validation_rejects_parent_segment(self) -> None:
        with self.assertRaises(AuthorityError) as ctx:
            validate_paths(["../escape"], [])
        self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_path_validation_rejects_empty(self) -> None:
        with self.assertRaises(AuthorityError) as ctx:
            validate_paths(["", "real.py"], [])
        self.assertEqual(ctx.exception.code, "VALIDATION_FAILED")

    def test_extract_task_block_requires_exact_id(self) -> None:
        tmp = Path(tempfile.mkdtemp())
        head = init_git_repo(tmp)
        docs = tmp / "docs" / "superpowers" / "plans" / "2026-08-05-m1-dry-run-foundation"
        docs.mkdir(parents=True)
        write_status(tmp, "docs/superpowers/plans/2026-08-05-m1-dry-run-foundation", "T-TEST-01")
        write_packet(docs)
        block = toml_task_block(head)
        write_build_tasks(docs, block)

        with self.assertRaises(AuthorityError) as ctx:
            extract_task_block(tmp, docs, "T-NO-SUCH-ID")
        self.assertEqual(ctx.exception.code, "AUTHORITY_MISSING")


if __name__ == "__main__":
    unittest.main()
