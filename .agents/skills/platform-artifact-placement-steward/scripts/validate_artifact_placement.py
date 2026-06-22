#!/usr/bin/env python3
"""Validate a simple artifact placement manifest.

This script intentionally validates only broad placement prefixes. It is a helper,
not a replacement for architecture review.

Usage:
    python validate_artifact_placement.py artifact-manifest.json

Manifest shape:
[
  {
    "artifact": "ProcedureStatus.cs",
    "artifact_type": "domain enum",
    "path": "src/Modules/Medical/Domain/Enums/ProcedureStatus.cs"
  }
]
"""

from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class PlacementRule:
    keywords: tuple[str, ...]
    allowed_prefix_fragments: tuple[str, ...]
    description: str


RULES: tuple[PlacementRule, ...] = (
    PlacementRule(("domain", "entity", "enum", "pure model", "pure rule"), ("src/Modules/", "/Domain"), "Domain artifacts belong under src/Modules/<Module>/Domain."),
    PlacementRule(("dto", "descriptor", "read dto", "write dto"), ("src/Modules/", "/Domain/"), "DTOs/descriptors follow the module Domain/Dto, Domain/Models, or existing convention."),
    PlacementRule(("command",), ("src/Modules/", "/Application/Commands"), "Commands belong under Application/Commands."),
    PlacementRule(("query",), ("src/Modules/", "/Application/Queries"), "Queries belong under Application/Queries."),
    PlacementRule(("contract", "port", "abstraction"), ("src/Modules/", "/Application/"), "Application contracts/ports belong under Application/Abstractions or Application/Contracts."),
    PlacementRule(("event", "application event"), ("src/Modules/", "/Application/Events"), "Application events belong under Application/Events."),
    PlacementRule(("infrastructure", "adapter"), ("src/Modules/", "/Infrastructure"), "Infrastructure adapters belong under Infrastructure."),
    PlacementRule(("repository",), ("src/Modules/", "/Infrastructure/Repositories"), "Repositories belong under Infrastructure/Repositories."),
    PlacementRule(("dbcontext", "data mapping", "ef", "migration"), ("src/Modules/", "/Infrastructure/Data"), "DbContext/data mapping belongs under Infrastructure/Data."),
    PlacementRule(("background job", "worker"), ("src/Modules/", "/Infrastructure/BackgroundJobs"), "Background jobs belong under Infrastructure/BackgroundJobs."),
    PlacementRule(("component", "reusable module ui"), ("src/Modules/", "/Interface/Components"), "Reusable module UI belongs under Interface/Components."),
    PlacementRule(("interface model", "interface service", "ui-facing"), ("src/Modules/", "/Interface/"), "UI-facing interface models/services belong under Interface."),
    PlacementRule(("module di", "dependency injection"), ("src/Modules/", "/DependencyInjection"), "Module DI belongs in the owning layer dependency-injection folder or Module project."),
    PlacementRule(("module composition",), ("src/Modules/", "/Module"), "Module composition belongs under the module composition project/folder."),
    PlacementRule(("routable page", "host page", "page"), ("src/Hosts/Main/Pages",), "Routable host pages belong under src/Hosts/Main/Pages."),
    PlacementRule(("route", "route constants", "route builder"), ("src/Hosts/Main/Common/Routes",), "Host route constants/builders belong under src/Hosts/Main/Common/Routes."),
    PlacementRule(("shared platform", "primitive", "library"), ("src/Libraries", "src/Modules/Base"), "Shared platform primitives belong under src/Libraries or src/Modules/Base."),
    PlacementRule(("app-specific", "app artifact"), ("src/Apps/",), "App-specific artifacts belong under src/Apps/<App>."),
    PlacementRule(("identity",), ("src/Identity/",), "Identity artifacts belong under src/Identity/<BoundedContext>."),
    PlacementRule(("test",), ("tests/",), "Tests belong under tests/..."),
    PlacementRule(("documentation", "docs", "document"), ("docs/",), "Documentation belongs under docs/..."),
)


def normalize(value: str) -> str:
    return value.strip().lower().replace("_", "-")


def find_rule(artifact_type: str) -> PlacementRule | None:
    normalized = normalize(artifact_type)
    for rule in RULES:
        if any(keyword in normalized for keyword in rule.keywords):
            return rule
    return None


def path_matches(path: str, rule: PlacementRule) -> bool:
    normalized_path = path.replace("\\", "/")
    fragments = rule.allowed_prefix_fragments

    # Multiple fragments in one rule can mean either alternatives or required pair.
    # If the first fragment is src/Modules and the second is /Domain, require both.
    if len(fragments) == 2 and fragments[0].endswith("/") and fragments[1].startswith("/"):
        return normalized_path.startswith(fragments[0]) and fragments[1] in normalized_path

    return any(normalized_path.startswith(fragment) or fragment in normalized_path for fragment in fragments)


def validate(items: Iterable[dict]) -> int:
    errors = 0
    warnings = 0

    for index, item in enumerate(items, start=1):
        artifact = item.get("artifact") or f"item #{index}"
        artifact_type = item.get("artifact_type") or ""
        path = item.get("path") or ""

        if not artifact_type or not path:
            print(f"ERROR: {artifact}: missing artifact_type or path")
            errors += 1
            continue

        rule = find_rule(artifact_type)
        if rule is None:
            print(f"WARN:  {artifact}: unknown artifact_type '{artifact_type}'. Human classification required.")
            warnings += 1
            continue

        if path_matches(path, rule):
            print(f"OK:    {artifact}: {path}")
        else:
            print(f"ERROR: {artifact}: path '{path}' does not match '{artifact_type}'. {rule.description}")
            errors += 1

    print(f"\nSummary: {errors} error(s), {warnings} warning(s)")
    return 1 if errors else 0


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2

    manifest_path = Path(sys.argv[1])
    try:
        items = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001 - script should show direct user-facing failure.
        print(f"ERROR: failed to read manifest: {exc}")
        return 2

    if not isinstance(items, list):
        print("ERROR: manifest root must be a JSON array")
        return 2

    return validate(items)


if __name__ == "__main__":
    raise SystemExit(main())
