#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

REQUIRED_SECTIONS = [
    "## 1. Objective",
    "## 2. Scope",
    "## 3. Code / Risk Analysis",
    "## 4. Required Unit Tests",
    "## 6. Required Hot Test Scenarios",
    "## 11. CI / Execution Guidance",
    "## 12. Traceability",
]

METADATA_KEYS = [
    "Date:",
    "Status:",
    "App:",
    "Module:",
    "Capability:",
    "Owner:",
    "Confidence:",
]


def validate_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    errors: list[str] = []
    for key in METADATA_KEYS:
        if key not in text:
            errors.append(f"{path}: missing metadata {key}")
    for section in REQUIRED_SECTIONS:
        if section not in text:
            errors.append(f"{path}: missing section {section}")
    if "Hot-Path" in text or "hot-path" in text:
        errors.append(f"{path}: use 'Hot' or 'Hot Test Scenario' instead of hot-path terminology")
    if "dotnet test" in text and "Hot" in text:
        # This is a warning-like check: allow it only when environment guidance is present.
        if "environment" not in text.lower() and "scenario" not in text.lower():
            errors.append(f"{path}: dotnet test mentioned without scenario/environment guidance")
    return errors


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".")
    files = [p for p in root.rglob("*-test-plan.md") if p.is_file()]
    errors: list[str] = []
    for file in files:
        errors.extend(validate_file(file))
    if errors:
        print("Test plan validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1
    print(f"Validated {len(files)} test plan file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
