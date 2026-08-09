#!/usr/bin/env python3
"""Generate machine-readable and Markdown task indices from the master plan."""

from __future__ import annotations

import csv
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / "plans" / "MASTER_IMPLEMENTATION_PLAN.md"
OUT_JSON = ROOT / "plans" / "TASK_INDEX.json"
OUT_CSV = ROOT / "plans" / "TASK_INDEX.csv"
OUT_MD = ROOT / "plans" / "TASK_INDEX.md"

PHASE_RE = re.compile(r"^# (SA\d{2}) — (.+)$", re.MULTILINE)
TASK_RE = re.compile(r"^## (SA\d{2}-T\d{2}) — (.+)$", re.MULTILINE)


def normalize(value: str | None, default: str) -> str:
    if value is None:
        return default
    return value.strip().rstrip(".")


def extract_field(body: str, names: list[str]) -> str | None:
    escaped = "|".join(re.escape(name) for name in names)
    match = re.search(rf"\*\*(?:{escaped}):\*\*\s*(.+)", body)
    return match.group(1).strip() if match else None


def main() -> None:
    text = PLAN.read_text(encoding="utf-8")
    phases = list(PHASE_RE.finditer(text))
    tasks = list(TASK_RE.finditer(text))

    phase_for_position: list[tuple[int, str, str]] = [
        (m.start(), m.group(1), m.group(2).strip()) for m in phases
    ]

    records: list[dict[str, str]] = []
    for index, match in enumerate(tasks):
        start = match.end()
        end = tasks[index + 1].start() if index + 1 < len(tasks) else len(text)
        # Stop at a phase heading that appears before the next task.
        later_phases = [p for p in phases if p.start() > match.start() and p.start() < end]
        if later_phases:
            end = later_phases[0].start()
        body = text[start:end]

        phase = max((p for p in phase_for_position if p[0] < match.start()), key=lambda p: p[0])
        task_id = match.group(1)
        title = match.group(2).strip()
        prereq = normalize(extract_field(body, ["Prerequisites", "Prerequisite"]), "none")
        outcome = normalize(extract_field(body, ["Required outcome"]), "See master plan section")
        packet_rel = f"plans/tasks/{task_id}.md"
        packet_abs = ROOT / packet_rel
        kind = "gate" if re.search(r"\bgate\s+A\d+\b", title, re.IGNORECASE) else "task"
        records.append(
            {
                "task_id": task_id,
                "title": title,
                "slice_id": phase[1],
                "slice_title": phase[2],
                "kind": kind,
                "prerequisites": prereq,
                "required_outcome": outcome,
                "packet_path": packet_rel if packet_abs.exists() else "compile-at-dispatch",
            }
        )

    if len(records) != 97:
        raise SystemExit(f"Expected 97 tasks, parsed {len(records)}")
    if len({r['task_id'] for r in records}) != len(records):
        raise SystemExit("Duplicate task IDs found")

    OUT_JSON.write_text(
        json.dumps(
            {
                "schema_version": "square.task-index/v1",
                "task_count": len(records),
                "tasks": records,
            },
            indent=2,
            ensure_ascii=False,
        )
        + "\n",
        encoding="utf-8",
    )

    with OUT_CSV.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(records[0].keys()))
        writer.writeheader()
        writer.writerows(records)

    lines = [
        "# Task Index",
        "",
        f"Total tasks: **{len(records)}**.",
        "",
        "Detailed packets are supplied for SA00 and SA01. Later packets must be compiled immediately before dispatch from the accepted start commit and inspected pinned-source symbols; this prevents stale paths from becoming authority.",
        "",
    ]
    for phase_id, phase_title in [(p[1], p[2]) for p in phase_for_position]:
        phase_records = [r for r in records if r["slice_id"] == phase_id]
        lines.extend(
            [
                f"## {phase_id} — {phase_title}",
                "",
                "| Task | Title | Prerequisites | Detailed packet |",
                "|---|---|---|---|",
            ]
        )
        for record in phase_records:
            if record["packet_path"] == "compile-at-dispatch":
                packet = "Compile at dispatch"
            else:
                rel_from_plans = Path(record["packet_path"]).relative_to("plans")
                packet = f"[{rel_from_plans.name}]({rel_from_plans.as_posix()})"
            lines.append(
                f"| `{record['task_id']}` | {record['title']} | {record['prerequisites']} | {packet} |"
            )
        lines.append("")

    OUT_MD.write_text("\n".join(lines), encoding="utf-8")
    print(f"Generated {len(records)} tasks")


if __name__ == "__main__":
    main()
