import subprocess
from pathlib import Path


def init_git_repo(path: Path) -> str:
    subprocess.run(["git", "init", "-b", "main"], cwd=path, capture_output=True, check=True)
    subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=path, capture_output=True, check=True)
    subprocess.run(["git", "config", "user.name", "Test"], cwd=path, capture_output=True, check=True)
    (path / ".gitkeep").write_text("")
    subprocess.run(["git", "add", ".gitkeep"], cwd=path, capture_output=True, check=True)
    subprocess.run(["git", "commit", "-m", "init"], cwd=path, capture_output=True, check=True)
    result = subprocess.run(["git", "rev-parse", "HEAD"], cwd=path, capture_output=True, text=True, check=True)
    return result.stdout.strip()


def write_status(
    path: Path,
    packet_rel: str,
    task_id: str | None = None,
    *,
    worktree_state: str = "dirty",
) -> None:
    content = f"# Status\n\nActive planning subplan: `{packet_rel}`\n"
    if task_id:
        content += f"\nApplication implementation authorized: **yes — {task_id} only**\n"
    content += f"\nWorktree state: **{worktree_state}**\n"
    (path / "STATUS.md").write_text(content, encoding="utf-8")


def write_packet(packet_dir: Path) -> None:
    (packet_dir / "PACKET.md").write_text("# Packet\n\nTest packet content.\n", encoding="utf-8")
    (packet_dir / "BUILD.md").write_text("# Build\n\nTest build content.\n", encoding="utf-8")


def write_context_pairs(path: Path) -> None:
    for directory in (path, path / "docs", path / "docs" / "superpowers", path / "docs" / "superpowers" / "plans"):
        directory.mkdir(exist_ok=True)
        (directory / "AGENTS.md").write_text("# Context\n", encoding="utf-8")
        (directory / "CLAUDE.md").write_text("# Context\n", encoding="utf-8")


def toml_task_block(head_sha: str, **overrides: object) -> str:
    fields: dict[str, object] = {
        "schema": 1,
        "id": '"T-TEST-01"',
        "role": '"IMPLEMENT"',
        "mode": '"write"',
        "starting_commit": f'"{head_sha}"',
        "allowed_paths": '["sqorch/"]',
        "forbidden_paths": '["tests/"]',
        "validation": '["python -m unittest"]',
        "expected_commit_message": '"feat: test"',
        "external_call_limit": 0,
        "spend_limit_usd": 0,
        "turn_limit": 100,
        "token_rotation_limit": 150000,
        "client": '"cmdc"',
        "model": '"deepseek/deepseek-v4-pro"',
        "automatic_fallback": "false",
        "evidence_destination": '"docs/STATE.md"',
        "acceptance_authority": '"owner"',
    }
    fields.update(overrides)
    lines = []
    for k, v in fields.items():
        lines.append(f'{k} = {v}')
    return "\n".join(lines)


def write_build_tasks(packet_dir: Path, *task_blocks: str) -> None:
    parts = ["# Build Tasks\n"]
    for block in task_blocks:
        parts.append("<!-- sqorch:task v1 -->\n```toml\n")
        parts.append(block)
        parts.append("\n```\n<!-- /sqorch:task -->\n")
    (packet_dir / "BUILD-TASKS.md").write_text("".join(parts), encoding="utf-8")
