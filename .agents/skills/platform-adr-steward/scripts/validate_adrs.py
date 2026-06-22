#!/usr/bin/env python3
"""Validate basic ADR hygiene across scoped app/module ADR folders."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

VALID_STATUSES = {"Proposed", "Accepted", "Rejected", "Deprecated", "Superseded"}
REQUIRED_METADATA = ["Date", "Status", "App", "Module", "Capability", "Owner"]
REQUIRED_SECTIONS = [
    "## Context",
    "## Decision",
    "## Consequences",
    "## Alternatives Considered",
    "## Validation / Enforcement",
    "## Notes",
]
ADR_FILE_RE = re.compile(r"^(?:ADR-)?(\d{4})-[a-z0-9]+(?:-[a-z0-9]+)*\.md$")
TITLE_RE = re.compile(r"^# ADR-(\d{4}): .+", re.MULTILINE)
METADATA_RE = re.compile(r"^(?P<key>[A-Za-z /]+):\s*(?P<value>.*)$", re.MULTILINE)


def metadata(text: str) -> dict[str, str]:
    return {m.group("key").strip(): m.group("value").strip() for m in METADATA_RE.finditer(text)}


def validate_file(path: Path) -> list[str]:
    errors: list[str] = []
    match = ADR_FILE_RE.match(path.name)
    if not match:
        errors.append(f"{path}: filename must match ADR-NNNN-kebab-case-title.md or NNNN-kebab-case-title.md")
        return errors

    number = match.group(1)
    text = path.read_text(encoding="utf-8")

    title = TITLE_RE.search(text)
    if not title:
        errors.append(f"{path}: missing title '# ADR-{number}: Title'")
    elif title.group(1) != number:
        errors.append(f"{path}: title ADR number {title.group(1)} does not match filename {number}")

    meta = metadata(text)
    for key in REQUIRED_METADATA:
        if key not in meta:
            errors.append(f"{path}: missing metadata '{key}:'")

    status = meta.get("Status")
    if status and status not in VALID_STATUSES:
        errors.append(f"{path}: invalid Status '{status}'")

    for section in REQUIRED_SECTIONS:
        if section not in text:
            errors.append(f"{path}: missing section '{section}'")

    if "/decisions/" not in path.as_posix() and path.parent.name != "decisions":
        errors.append(f"{path}: ADR should live under a decisions/ folder")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate basic scoped ADR hygiene.")
    parser.add_argument("directory", nargs="?", default="docs")
    args = parser.parse_args()

    root = Path(args.directory)
    if not root.exists():
        print(f"ERROR: path not found: {root}", file=sys.stderr)
        return 2

    adr_files = sorted(
        p for p in root.rglob("*.md")
        if p.name not in {"_index.md", "_template.md"} and (p.parent.name == "decisions" or p.name.startswith("ADR-"))
    )
    errors: list[str] = []

    seen: dict[str, Path] = {}
    for path in adr_files:
        match = ADR_FILE_RE.match(path.name)
        if match:
            adr_id = match.group(1)
            if adr_id in seen:
                errors.append(f"ADR number {adr_id} appears in both {seen[adr_id]} and {path}. Use global numbering or configure per-module numbering explicitly.")
            else:
                seen[adr_id] = path
        errors.extend(validate_file(path))

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(f"OK: validated {len(adr_files)} ADR file(s) under {root}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
