#!/usr/bin/env python3
"""Validate the Square session-first planning pack without network access."""

from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ERRORS: list[str] = []
WARNINGS: list[str] = []


def error(message: str) -> None:
    ERRORS.append(message)


def warning(message: str) -> None:
    WARNINGS.append(message)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_json_files() -> None:
    for path in sorted(ROOT.rglob("*.json")):
        try:
            json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:  # noqa: BLE001
            error(f"Invalid JSON {path.relative_to(ROOT)}: {exc}")


def validate_task_index() -> None:
    path = ROOT / "plans" / "TASK_INDEX.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    tasks = data.get("tasks", [])
    if data.get("task_count") != 97 or len(tasks) != 97:
        error(f"Task index must contain 97 tasks; got {len(tasks)}")
    ids = [task.get("task_id") for task in tasks]
    if len(ids) != len(set(ids)):
        error("Task index contains duplicate IDs")
    expected = [f"SA{phase:02d}-T{task:02d}" for phase in range(16) for task in []]
    del expected  # IDs are intentionally non-uniform by phase; uniqueness/count are authoritative.
    for task in tasks:
        packet = task.get("packet_path")
        if packet and packet != "compile-at-dispatch" and not (ROOT / packet).is_file():
            error(f"Missing detailed packet {packet} for {task.get('task_id')}")


def validate_master_plan() -> None:
    text = (ROOT / "plans" / "MASTER_IMPLEMENTATION_PLAN.md").read_text(encoding="utf-8")
    ids = re.findall(r"^## (SA\d{2}-T\d{2}) —", text, flags=re.MULTILINE)
    if len(ids) != 97:
        error(f"Master plan must contain 97 task headings; got {len(ids)}")
    if len(ids) != len(set(ids)):
        error("Master plan has duplicate task IDs")
    for criterion in range(1, 31):
        key = f"SF-AC-{criterion:02d}"
        if key not in text:
            error(f"Missing cross-cutting criterion {key}")


def validate_schema_fixtures() -> None:
    schema_dir = ROOT / "schemas"
    fixture_dir = ROOT / "fixtures"
    try:
        from jsonschema import Draft202012Validator  # type: ignore
        from referencing import Registry, Resource  # type: ignore
    except Exception:  # noqa: BLE001
        warning("jsonschema/referencing is not installed; full fixture validation was skipped")
        return

    schemas = {
        path.name: json.loads(path.read_text(encoding="utf-8"))
        for path in sorted(schema_dir.glob("*.schema.json"))
    }
    registry = Registry()
    for name, schema in schemas.items():
        try:
            Draft202012Validator.check_schema(schema)
            schema_id = schema.get("$id")
            if isinstance(schema_id, str):
                registry = registry.with_resource(schema_id, Resource.from_contents(schema))
        except Exception as exc:  # noqa: BLE001
            error(f"Invalid schema {name}: {exc}")

    session_schema = schemas.get("session-read-model-v1.schema.json")
    if session_schema is None:
        error("Missing session-read-model-v1.schema.json")
        return
    validator = Draft202012Validator(session_schema, registry=registry)
    try:
        for path in sorted(fixture_dir.glob("*.json")):
            instance = json.loads(path.read_text(encoding="utf-8"))
            found = sorted(validator.iter_errors(instance), key=lambda item: list(item.path))
            for item in found:
                location = "/".join(map(str, item.absolute_path)) or "<root>"
                error(f"Fixture {path.name} at {location}: {item.message}")
    except Exception as exc:  # noqa: BLE001
        error(f"Fixture schema resolution failed: {exc}")


def validate_references() -> None:
    required = [
        "README.md",
        "SOURCES.md",
        "plans/START_HERE.md",
        "plans/OWNER_ACCEPTANCE_CHECKLIST.md",
        "plans/MASTER_IMPLEMENTATION_PLAN.md",
        "plans/TASK_INDEX.md",
        "docs/ARCHITECTURE_AMENDMENT.md",
        "docs/SESSION_DOMAIN_MODEL.md",
        "docs/ROLE_ROUTING_MODEL_SELECTION.md",
        "docs/PERSISTENCE_AND_EVENTS.md",
        "docs/API_AND_EXECUTION_FACADE.md",
        "docs/SESSION_FIRST_UI_SPEC.md",
        "ui/square-session-workspace-rounded-reference.html",
        "plans/KICKOFF_PROMPT_SA00-T01.md",
    ]
    for relative in required:
        if not (ROOT / relative).is_file():
            error(f"Missing required pack file: {relative}")
    for task_id in [*(f"SA00-T{i:02d}" for i in range(1, 6)), *(f"SA01-T{i:02d}" for i in range(1, 7))]:
        if not (ROOT / "plans" / "tasks" / f"{task_id}.md").is_file():
            error(f"Missing ready-to-dispatch packet: {task_id}")


def validate_scripts() -> None:
    bootstrap = (ROOT / "scripts" / "bootstrap-square-fork.ps1").read_text(encoding="utf-8")
    if "Invoke-Git -C" in bootstrap:
        error("bootstrap script contains unsafe/unbound Invoke-Git -C invocation")
    if "No commit and no remote push were performed" not in bootstrap:
        error("bootstrap script must explicitly report no commit/push")
    baseline = (ROOT / "scripts" / "verify-ao-baseline.ps1").read_text(encoding="utf-8")
    if "Where-Object { $_.Name -ne 'manifest.sha256' }" not in baseline:
        error("baseline manifest generation must exclude itself")


def validate_markdown_links() -> None:
    link_re = re.compile(r"\[[^\]]+\]\(([^)]+)\)")
    for path in sorted(ROOT.rglob("*.md")):
        text = path.read_text(encoding="utf-8")
        for raw in link_re.findall(text):
            target = raw.split("#", 1)[0].strip()
            if not target or target.startswith(("http://", "https://", "mailto:", "#", "<")):
                continue
            if any(char in target for char in ("<", ">", "*")):
                continue
            resolved = (path.parent / target).resolve()
            try:
                resolved.relative_to(ROOT.resolve())
            except ValueError:
                continue
            if not resolved.exists():
                error(f"Broken local link in {path.relative_to(ROOT)}: {raw}")


def validate_no_generated_outputs() -> None:
    blocked = {"node_modules", "dist", "bin", "obj", "__pycache__", ".pytest_cache"}
    for path in ROOT.rglob("*"):
        if path.is_dir() and path.name in blocked:
            error(f"Generated/cache directory included: {path.relative_to(ROOT)}")


def validate_ui_reference() -> None:
    path = ROOT / "ui" / "square-session-workspace-rounded-reference.html"
    text = path.read_text(encoding="utf-8")
    if "<html" not in text.lower() or "</html>" not in text.lower():
        error("Approved UI reference is not complete HTML")
    for match in re.finditer(r"\b(?:src|href)=[\"']([^\"']+)[\"']", text, flags=re.IGNORECASE):
        value = match.group(1).strip()
        if value.startswith(("http://", "https://", "//")):
            error(f"Approved UI reference has an external resource: {value}")

def validate_manifest() -> None:
    manifest = ROOT / "MANIFEST.sha256"
    if not manifest.exists():
        warning("MANIFEST.sha256 does not exist yet")
        return
    listed: set[str] = set()
    for line_no, line in enumerate(manifest.read_text(encoding="ascii").splitlines(), 1):
        if not line.strip():
            continue
        match = re.fullmatch(r"([0-9a-f]{64})  (.+)", line)
        if not match:
            error(f"Malformed manifest line {line_no}")
            continue
        expected, relative = match.groups()
        listed.add(relative)
        path = ROOT / relative
        if not path.is_file():
            error(f"Manifest references missing file: {relative}")
        elif sha256(path) != expected:
            error(f"Manifest hash mismatch: {relative}")
    actual = {
        path.relative_to(ROOT).as_posix()
        for path in ROOT.rglob("*")
        if path.is_file() and path.name != "MANIFEST.sha256"
    }
    missing = sorted(actual - listed)
    extra = sorted(listed - actual)
    if missing:
        error(f"Manifest omits {len(missing)} files: {missing[:5]}")
    if extra:
        error(f"Manifest has {len(extra)} extra files: {extra[:5]}")


def main() -> int:
    validate_json_files()
    validate_master_plan()
    validate_task_index()
    validate_schema_fixtures()
    validate_references()
    validate_scripts()
    validate_markdown_links()
    validate_no_generated_outputs()
    validate_ui_reference()
    validate_manifest()

    for message in WARNINGS:
        print(f"WARNING: {message}")
    for message in ERRORS:
        print(f"ERROR: {message}")
    if ERRORS:
        print(f"FAILED: {len(ERRORS)} error(s), {len(WARNINGS)} warning(s)")
        return 1
    print(f"PASS: pack validated with {len(WARNINGS)} warning(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
