#!/usr/bin/env python3
"""Lightweight scoped roadmap/task markdown validator."""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

ROADMAP_REQUIRED = [
    "App:",
    "Module:",
    "Capability:",
    "## 1. Objective",
    "## 2. Background",
    "## 4. Success Criteria",
    "## 5. Scope",
    "## 6. Roadmap Summary",
    "## 7. Phased Delivery Plan",
    "## 8. Artifact Map",
    "## 9. Feature Map",
    "## 13. Traceability",
]

TASK_REQUIRED = [
    "# Implementation Tasks:",
    "Source roadmap:",
    "Roadmap status:",
    "App:",
    "Module:",
    "Capability:",
    "## Task Summary",
    "## Tickets",
    "#### Acceptance Criteria",
    "#### Validation",
]

VALID_ROADMAP_STATUSES = {"Draft", "Proposed", "Approved", "Deprecated", "Superseded"}
VALID_EXEC_STATUSES = {"Draft", "Ready"}


def validate_file(path: pathlib.Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    errors: list[str] = []

    if "roadmap" in path.name or path.parent.name == "roadmaps":
        if path.parent.name != "roadmaps":
            errors.append("roadmap should live under a roadmaps/ folder")
        for required in ROADMAP_REQUIRED:
            if required not in text:
                errors.append(f"missing roadmap section/field: {required}")
        status = re.search(r"^Status:\s*(.+)$", text, re.MULTILINE)
        if status and status.group(1).strip() not in VALID_ROADMAP_STATUSES:
            errors.append(f"invalid roadmap status: {status.group(1).strip()}")

    if "task" in path.name or path.parent.name == "tasks":
        if path.parent.name != "tasks":
            errors.append("task file should live under a tasks/ folder")
        for required in TASK_REQUIRED:
            if required not in text:
                errors.append(f"missing task section/field: {required}")
        execution = re.search(r"^Execution status:\s*(.+)$", text, re.MULTILINE)
        if execution and execution.group(1).strip() not in VALID_EXEC_STATUSES:
            errors.append(f"invalid execution status: {execution.group(1).strip()}")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="*", default=["docs"], help="Roadmap/task markdown files or directories")
    args = parser.parse_args()

    files: list[pathlib.Path] = []
    for value in args.paths:
        path = pathlib.Path(value)
        if path.is_dir():
            files.extend(
                p for p in path.rglob("*.md")
                if not p.name.startswith("_") and (p.parent.name in {"roadmaps", "tasks"} or "roadmap" in p.name or "task" in p.name)
            )
        else:
            files.append(path)

    failed = False
    for file in sorted(files):
        errors = validate_file(file)
        if errors:
            failed = True
            print(f"{file}:")
            for error in errors:
                print(f"  - {error}")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
