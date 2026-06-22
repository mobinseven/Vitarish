#!/usr/bin/env python3
"""Validate Vitarish journey map markdown files across scoped app/module folders."""

from __future__ import annotations

import sys
from pathlib import Path

REQUIRED_SECTIONS = [
    "# ",
    "## 1. Context",
    "## 2. Persona Snapshot",
    "## 3. Journey Summary",
    "## 4. Journey Stages",
    "## 5. Cross-Journey Insights",
]

RECOMMENDED_SECTIONS = [
    "## 6. Implementation Derivation",
    "## 7. Technical Detail Addendum",
]

REQUIRED_FIELDS = [
    "App:",
    "Module:",
    "Capability:",
    "Product / service:",
    "Journey type:",
    "Scenario:",
    "User goal:",
    "Business goal:",
    "Scope start:",
    "Scope end:",
    "Primary persona:",
    "Evidence sources:",
    "Confidence level:",
    "**Name / segment:**",
    "**Situation:**",
    "**Motivation:**",
    "**Key needs:**",
    "**Constraints:**",
    "**Success means:**",
    "**Narrative:**",
    "**Emotional arc:**",
    "**Biggest friction point:**",
    "**Highest-value opportunity:**",
]


def validate_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    errors: list[str] = []

    for section in REQUIRED_SECTIONS:
        if section not in text:
            errors.append(f"missing required section: {section}")

    for field in REQUIRED_FIELDS:
        if field not in text:
            errors.append(f"missing required field: {field}")

    if "### Stage " not in text:
        errors.append("missing at least one journey stage")

    for section in RECOMMENDED_SECTIONS:
        if section not in text:
            errors.append(f"missing recommended section: {section}")

    if "Evidence:" not in text:
        errors.append("missing stage evidence")

    if "Success metrics:" not in text:
        errors.append("missing stage success metrics")

    if path.parent.name != "journeys":
        errors.append("journey should live under a journeys/ folder")

    return errors


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("docs")

    if not root.exists():
        print(f"Path not found: {root}", file=sys.stderr)
        return 2

    files = sorted(
        p for p in root.rglob("*.md")
        if not p.name.startswith("_") and (p.parent.name == "journeys" or "journey" in p.name)
    )

    if not files:
        print(f"No journey markdown files found under {root}")
        return 0

    has_errors = False

    for path in files:
        errors = validate_file(path)
        if errors:
            has_errors = True
            print(f"\n{path}:")
            for error in errors:
                print(f"  - {error}")

    if has_errors:
        print("\nJourney validation failed.")
        return 1

    print(f"Journey validation passed for {len(files)} file(s).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
