#!/usr/bin/env python3
"""Validate ADR-0003 module Interface dependency boundaries."""
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

BACKEND_LAYERS = ("Application", "Domain", "Infrastructure", "Api")
SERVER_ONLY_PROJECT_FRAGMENTS = ("src/Libraries/Entities/Entities.Server",)
SERVER_ONLY_NAMESPACE_FRAGMENTS = ("Entities.Server",)
MODES = {"ReportOnly", "FailFast"}


@dataclass(frozen=True)
class Violation:
    module: str
    mode: str
    kind: str
    path: Path
    line: int
    forbidden: str
    reason: str


@dataclass(frozen=True)
class SuppressedViolation:
    violation: Violation
    exception_reason: str
    removal_condition: str


def repository_root() -> Path:
    return Path(__file__).resolve().parents[2]


def load_config(root: Path, config_path: Path) -> dict:
    data = json.loads(config_path.read_text(encoding="utf-8"))

    default_mode = data.get("defaultMode", "ReportOnly")
    if default_mode not in MODES:
        raise ValueError(f"defaultMode must be one of {sorted(MODES)}: {default_mode}")

    for module, settings in data.get("modules", {}).items():
        mode = settings.get("mode", default_mode)
        if mode not in MODES:
            raise ValueError(f"module {module} mode must be one of {sorted(MODES)}: {mode}")

        for exception in settings.get("temporaryExceptions", []):
            missing = [
                name
                for name in ("path", "forbidden", "reason", "removalCondition")
                if not exception.get(name)
            ]

            if missing:
                raise ValueError(
                    f"module {module} temporary exception is missing: {', '.join(missing)}"
                )

    return data


def module_mode(config: dict, module: str) -> str:
    return config.get("modules", {}).get(module, {}).get(
        "mode",
        config.get("defaultMode", "ReportOnly"),
    )


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def normalize_project_path(project_file: Path, include: str) -> Path:
    normalized = include.replace("\\", "/")
    return (project_file.parent / normalized).resolve()


def normalize_relative_path_value(value: str | Path) -> str:
    text = str(value).replace("\\", "/").strip()

    while text.startswith("./"):
        text = text[2:]

    return text


def classify_project_reference(
    root: Path,
    module: str,
    reference_path: Path,
) -> tuple[str, str] | None:
    try:
        relative = reference_path.relative_to(root).as_posix()
    except ValueError:
        relative = reference_path.as_posix()

    for fragment in SERVER_ONLY_PROJECT_FRAGMENTS:
        if relative == f"{fragment}.csproj" or relative.startswith(f"{fragment}/"):
            return (
                "Entities.Server",
                "Interface projects must not reference the server-only Entities library.",
            )

    match = re.match(r"src/Modules/([^/]+)/([^/]+)/[^/]+\.csproj$", relative)
    if not match:
        return None

    referenced_module, layer = match.groups()
    if layer not in BACKEND_LAYERS:
        return None

    scope = "same-module" if referenced_module == module else "cross-module"

    return (
        f"{scope} {referenced_module}.{layer}",
        f"Interface projects must not reference {scope} backend {layer} projects.",
    )


def scan_project(
    root: Path,
    project_file: Path,
    module: str,
    mode: str,
) -> list[Violation]:
    violations: list[Violation] = []

    text = project_file.read_text(encoding="utf-8")
    xml = ET.fromstring(text)

    for item in xml.iter():
        if item.tag.split("}")[-1] != "ProjectReference":
            continue

        include = item.attrib.get("Include")
        if not include:
            continue

        classification = classify_project_reference(
            root,
            module,
            normalize_project_path(project_file, include),
        )

        if not classification:
            continue

        forbidden, reason = classification
        offset = text.find(include)

        violations.append(
            Violation(
                module=module,
                mode=mode,
                kind="ProjectReference",
                path=project_file.relative_to(root),
                line=line_number(text, max(offset, 0)),
                forbidden=forbidden,
                reason=reason,
            )
        )

    return violations


def namespace_patterns(modules: list[str]) -> list[tuple[re.Pattern[str], str, str]]:
    patterns: list[tuple[re.Pattern[str], str, str]] = []

    for module in modules:
        for layer in BACKEND_LAYERS:
            token = (
                rf"(?:^|[^A-Za-z0-9_])"
                rf"(?:Vitarish\.)?Modules\.{module}\.{layer}"
                rf"(?:\b|\.)"
            )

            patterns.append(
                (
                    re.compile(token),
                    f"Modules.{module}.{layer}",
                    f"Interface source must not import or use backend {layer} namespaces.",
                )
            )

            short_token = rf"(?:^|[^A-Za-z0-9_]){module}\.{layer}(?:\b|\.)"

            patterns.append(
                (
                    re.compile(short_token),
                    f"{module}.{layer}",
                    f"Interface source must not import or use backend {layer} namespaces.",
                )
            )

    for fragment in SERVER_ONLY_NAMESPACE_FRAGMENTS:
        patterns.append(
            (
                re.compile(rf"(?:^|[^A-Za-z0-9_]){re.escape(fragment)}(?:\b|\.)"),
                fragment,
                "Interface source must not import or use server-only Entities namespaces.",
            )
        )

    return patterns


def violation_kind(path: Path, text: str, match_start: int) -> str:
    suffix = path.suffix.lower()
    prefix = text[
        max(0, text.rfind("\n", 0, match_start) + 1) : match_start
    ].strip()

    using_like = (
        prefix.startswith("using")
        or prefix.startswith("global using")
        or prefix.startswith("@using")
    )

    if suffix == ".razor":
        return "RazorNamespaceImport" if using_like else "RazorFullyQualifiedUsage"

    return "CSharpNamespaceImport" if using_like else "CSharpFullyQualifiedUsage"


def scan_source(
    root: Path,
    source_file: Path,
    module: str,
    mode: str,
    patterns: list[tuple[re.Pattern[str], str, str]],
) -> list[Violation]:
    text = source_file.read_text(encoding="utf-8")
    violations: list[Violation] = []
    seen: set[tuple[int, str]] = set()

    for pattern, forbidden, reason in patterns:
        for match in pattern.finditer(text):
            key = (line_number(text, match.start()), forbidden)

            if key in seen:
                continue

            seen.add(key)

            violations.append(
                Violation(
                    module=module,
                    mode=mode,
                    kind=violation_kind(source_file, text, match.start()),
                    path=source_file.relative_to(root),
                    line=key[0],
                    forbidden=forbidden,
                    reason=reason,
                )
            )

    return violations


def discover_modules(root: Path) -> list[str]:
    return sorted(
        path.parent.parent.name
        for path in root.glob("src/Modules/*/Interface/*.csproj")
    )


def collect_violations(root: Path, config: dict) -> list[Violation]:
    modules = discover_modules(root)
    patterns = namespace_patterns(modules)
    violations: list[Violation] = []

    for project_file in sorted(root.glob("src/Modules/*/Interface/*.csproj")):
        module = project_file.parent.parent.name
        mode = module_mode(config, module)

        violations.extend(scan_project(root, project_file, module, mode))

    source_files = sorted(root.glob("src/Modules/*/Interface/**/*.cs")) + sorted(
        root.glob("src/Modules/*/Interface/**/*.razor")
    )

    for source_file in source_files:
        module = source_file.relative_to(root).parts[2]
        mode = module_mode(config, module)

        violations.extend(scan_source(root, source_file, module, mode, patterns))

    return violations


def find_temporary_exception(config: dict, violation: Violation) -> dict | None:
    module_settings = config.get("modules", {}).get(violation.module, {})
    exceptions = module_settings.get("temporaryExceptions", [])

    violation_path = normalize_relative_path_value(violation.path)

    for exception in exceptions:
        exception_path = normalize_relative_path_value(exception.get("path", ""))
        exception_forbidden = exception.get("forbidden", "")

        if exception_path == violation_path and exception_forbidden == violation.forbidden:
            return exception

    return None


def filter_temporary_exceptions(
    config: dict,
    violations: list[Violation],
) -> tuple[list[Violation], list[SuppressedViolation]]:
    active: list[Violation] = []
    suppressed: list[SuppressedViolation] = []

    for violation in violations:
        exception = find_temporary_exception(config, violation)

        if exception:
            suppressed.append(
                SuppressedViolation(
                    violation=violation,
                    exception_reason=exception["reason"],
                    removal_condition=exception["removalCondition"],
                )
            )
            continue

        active.append(violation)

    return active, suppressed


def scan(root: Path, config: dict) -> list[Violation]:
    violations, _ = filter_temporary_exceptions(
        config,
        collect_violations(root, config),
    )

    return violations


def print_violation(violation: Violation) -> None:
    print(
        f"{violation.mode}: {violation.kind}: {violation.path}:{violation.line}: "
        f"module={violation.module}; "
        f"forbidden={violation.forbidden}; "
        f"reason={violation.reason}"
    )


def print_suppressed_violation(suppressed: SuppressedViolation) -> None:
    violation = suppressed.violation

    print(
        f"Suppressed temporary exception: {violation.kind}: "
        f"{violation.path}:{violation.line}: "
        f"module={violation.module}; "
        f"forbidden={violation.forbidden}; "
        f"reason={violation.reason}; "
        f"exceptionReason={suppressed.exception_reason}; "
        f"removalCondition={suppressed.removal_condition}"
    )


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate ADR-0003 module Interface boundaries."
    )
    parser.add_argument(
        "--config",
        default="tools/architecture/interface_boundary_config.json",
    )

    args = parser.parse_args()

    root = repository_root()
    config = load_config(root, root / args.config)

    all_violations = collect_violations(root, config)
    violations, suppressed = filter_temporary_exceptions(config, all_violations)

    fail_fast = [
        violation
        for violation in violations
        if violation.mode == "FailFast"
    ]

    print("ADR-0003 Interface boundary validation")
    print(f"Configuration: {args.config}")
    modules = discover_modules(root)
    fail_fast_modules = [module for module in modules if module_mode(config, module) == "FailFast"]

    print(f"Modules scanned: {', '.join(modules)}")
    print(f"Fail-fast modules: {', '.join(fail_fast_modules) if fail_fast_modules else "none"}")
    print(f"Violations found: {len(violations)}")
    print(f"Temporary exceptions suppressed: {len(suppressed)}")
    print(f"Fail-fast violations: {len(fail_fast)}")

    for violation in violations:
        print_violation(violation)

    for suppressed_violation in suppressed:
        print_suppressed_violation(suppressed_violation)

    if fail_fast:
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
