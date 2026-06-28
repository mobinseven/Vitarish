#!/usr/bin/env python3
"""Validate sync between test plans, test code, and source code.

Checks:
  doc-completeness    All test plans in docs/platform/modules pass validation
  doc-index-sync      Each test plan file is listed in its module _index.md
  code-to-doc-coverage  Public types in Contracts/Client/Api mentioned in test plans
  doc-to-test-mapping   Each module with test plans has a test project
  test-plan-staleness   Plan Date older than recent source commits
  boundary-regression   Module Interface .csproj has no backend references
  unused-test-projects  Test projects with zero [Fact]/[Theory] attributes
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

INTERFACE_REF_PATTERN = re.compile(
    r"(Application|Domain|Infrastructure|Api\.csproj)", re.IGNORECASE
)
# Matches: public class Foo, public sealed record Bar, public enum Baz, public static class Qux
CSCLASS_RE = re.compile(
    r"public\s+(?:sealed\s+|abstract\s+|static\s+|readonly\s+)*"
    r"(?:class|record|enum|interface|struct)\s+(\w+)"
)
TEST_ATTR_RE = re.compile(r"\[(?:Fact|Theory)\b")
# C# keywords and common generic types to skip
SKIP_TYPES = frozenset({
    "bool", "int", "string", "void", "object", "byte", "char", "decimal",
    "double", "float", "long", "short", "uint", "ulong", "ushort",
    "sbyte", "nint", "nuint", "null", "true", "false",
    "IReadOnlyList", "IReadOnlyDictionary", "IEnumerable", "IDictionary",
    "List", "Dictionary", "Task", "ValueTask", "CancellationToken",
    "Guid", "DateTimeOffset", "DateTime", "DateOnly", "TimeOnly",
    "TimeSpan", "Uri", "Exception",
})


def check_doc_completeness(root: Path) -> list[str]:
    """Validate test plans in docs/platform/modules/ only."""
    errors: list[str] = []
    required_sections = [
        "## 1. Objective",
        "## 2. Scope",
        "## 3. Code / Risk Analysis",
        "## 4. Required Unit Tests",
        "## 6. Required Hot Test Scenarios",
        "## 11. CI / Execution Guidance",
        "## 12. Traceability",
    ]
    metadata_keys = ["Date:", "Status:", "App:", "Module:", "Capability:", "Owner:", "Confidence:"]
    modules_dir = root / "docs" / "platform" / "modules"
    if not modules_dir.exists():
        return errors
    for plan in sorted(modules_dir.rglob("*-test-plan.md")):
        text = plan.read_text(encoding="utf-8")
        rel = plan.relative_to(root)
        for key in metadata_keys:
            if key not in text:
                errors.append(f"[error] {rel}: missing metadata {key}")
        for section in required_sections:
            if section not in text:
                errors.append(f"[error] {rel}: missing section {section}")
    return errors


def check_doc_index_sync(root: Path) -> list[str]:
    """Verify each test plan file appears in its module _index.md."""
    warnings: list[str] = []
    modules_dir = root / "docs" / "platform" / "modules"
    if not modules_dir.exists():
        return warnings
    for module_dir in sorted(modules_dir.iterdir()):
        if not module_dir.is_dir():
            continue
        tests_dir = module_dir / "tests"
        index_file = tests_dir / "_index.md"
        if not tests_dir.exists():
            continue
        plans = [f.name for f in tests_dir.iterdir()
                 if f.suffix == ".md" and f.name != "_index.md"]
        if not plans:
            continue
        if not index_file.exists():
            warnings.append(
                f"[warning] {module_dir.name}/tests: _index.md missing, "
                f"{len(plans)} plan(s) exist"
            )
            continue
        index_text = index_file.read_text(encoding="utf-8")
        for plan_name in plans:
            if plan_name not in index_text:
                warnings.append(
                    f"[warning] {module_dir.name}/tests/_index.md: "
                    f"{plan_name} not listed"
                )
    return warnings


def check_code_to_doc_coverage(root: Path) -> list[str]:
    """Scan public types in module Contracts/Client/Api; warn if not in test plans."""
    warnings: list[str] = []
    src_modules = root / "src" / "Modules"
    if not src_modules.exists():
        return warnings

    for module_dir in sorted(src_modules.iterdir()):
        if not module_dir.is_dir():
            continue

        module_plan_text = ""
        module_docs_dir = root / "docs" / "platform" / "modules" / module_dir.name.lower()
        if module_docs_dir.exists():
            for plan in module_docs_dir.rglob("*-test-plan.md"):
                module_plan_text += plan.read_text(encoding="utf-8").lower()

        for layer in ["Contracts", "Client", "Api"]:
            layer_dir = module_dir / layer
            if not layer_dir.exists():
                continue
            for cs_file in layer_dir.rglob("*.cs"):
                content = cs_file.read_text(encoding="utf-8")
                for match in CSCLASS_RE.finditer(content):
                    type_name = match.group(1)
                    if type_name.lower() in SKIP_TYPES:
                        continue
                    if len(type_name) < 3:
                        continue
                    if module_plan_text and type_name.lower() not in module_plan_text:
                        rel = cs_file.relative_to(root)
                        warnings.append(
                            f"[warning] {rel}: public type "
                            f"'{type_name}' not mentioned in test plans"
                        )
    return warnings


def check_doc_to_test_mapping(root: Path) -> list[str]:
    """Each module with test plans should have a test project."""
    warnings: list[str] = []
    modules_dir = root / "docs" / "platform" / "modules"
    tests_dir = root / "tests" / "Modules"
    if not modules_dir.exists():
        return warnings
    # Build a lowercase→actual-name map for test projects (strip .Tests suffix)
    test_projects = {}
    if tests_dir.exists():
        for d in tests_dir.iterdir():
            if d.is_dir():
                key = d.name.lower().replace(".tests", "")
                test_projects[key] = d.name
    for module_dir in sorted(modules_dir.iterdir()):
        if not module_dir.is_dir():
            continue
        module_name = module_dir.name
        tests_subdir = module_dir / "tests"
        if not tests_subdir.exists():
            continue
        plans = [f for f in tests_subdir.iterdir()
                 if f.suffix == ".md" and f.name != "_index.md"]
        if not plans:
            continue
        actual_project = test_projects.get(module_name.lower())
        if not actual_project:
            warnings.append(
                f"[warning] {module_name}: test plan(s) exist but no "
                f"tests/Modules/*{module_name}*/ project"
            )
            continue
        test_project = tests_dir / actual_project
        test_files = list(test_project.rglob("*Tests.cs"))
        if not test_files:
            warnings.append(
                f"[warning] {actual_project}: no *Tests.cs files found"
            )
    return warnings


def check_test_plan_staleness(root: Path) -> list[str]:
    """Compare test plan Date against git log of source files."""
    infos: list[str] = []
    modules_dir = root / "docs" / "platform" / "modules"
    if not modules_dir.exists():
        return infos
    for plan in sorted(modules_dir.rglob("*-test-plan.md")):
        text = plan.read_text(encoding="utf-8")
        date_match = re.search(r"Date:\s*(\d{4}-\d{2}-\d{2})", text)
        if not date_match:
            continue
        plan_date = date_match.group(1)
        source_match = re.search(r"Source code/files:\s*(.+)", text)
        if not source_match:
            continue
        source_paths = re.findall(r"`([^`]+)`", source_match.group(1))
        for sp in source_paths:
            full_path = root / sp
            if not full_path.exists():
                continue
            try:
                result = subprocess.run(
                    ["git", "log", "-1", "--format=%ai", "--", str(full_path)],
                    capture_output=True, text=True, cwd=root, timeout=10
                )
                last_commit = result.stdout.strip()[:10]
                if last_commit and last_commit > plan_date:
                    rel = plan.relative_to(root)
                    infos.append(
                        f"[info] {rel}: plan date {plan_date} older than "
                        f"last commit {last_commit} for {sp}"
                    )
            except (subprocess.TimeoutExpired, FileNotFoundError):
                pass
    return infos


def check_boundary_regression(root: Path) -> list[str]:
    """Module Interface .csproj must not reference backend layers."""
    errors: list[str] = []
    src_modules = root / "src" / "Modules"
    if not src_modules.exists():
        return errors
    for module_dir in sorted(src_modules.iterdir()):
        if not module_dir.is_dir():
            continue
        interface_csproj = module_dir / "Interface" / f"{module_dir.name}.Interface.csproj"
        if not interface_csproj.exists():
            continue
        try:
            tree = ET.parse(interface_csproj)
            for ref in tree.iter("ProjectReference"):
                include = ref.get("Include", "")
                if INTERFACE_REF_PATTERN.search(include):
                    rel = interface_csproj.relative_to(root)
                    errors.append(
                        f"[error] {rel}: references backend in '{include}'"
                    )
        except ET.ParseError:
            pass
    return errors


def check_unused_test_projects(root: Path) -> list[str]:
    """Find test projects with zero [Fact]/[Theory] attributes."""
    warnings: list[str] = []
    tests_dir = root / "tests" / "Modules"
    if not tests_dir.exists():
        return warnings
    for test_dir in sorted(tests_dir.iterdir()):
        if not test_dir.is_dir():
            continue
        test_files = list(test_dir.rglob("*.cs"))
        has_tests = any(
            TEST_ATTR_RE.search(tf.read_text(encoding="utf-8"))
            for tf in test_files
        )
        if not has_tests and test_files:
            warnings.append(
                f"[warning] {test_dir.name}: {len(test_files)} .cs file(s) "
                f"but zero [Fact]/[Theory] attributes"
            )
    return warnings


ALL_CHECKS = {
    "doc-completeness": check_doc_completeness,
    "doc-index-sync": check_doc_index_sync,
    "code-to-doc-coverage": check_code_to_doc_coverage,
    "doc-to-test-mapping": check_doc_to_test_mapping,
    "test-plan-staleness": check_test_plan_staleness,
    "boundary-regression": check_boundary_regression,
    "unused-test-projects": check_unused_test_projects,
}


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate test sync between docs, code, and tests"
    )
    parser.add_argument("root", nargs="?", default=".", help="Repository root")
    parser.add_argument("--checks", nargs="*", help="Run specific checks only")
    parser.add_argument(
        "--format", choices=["text", "json", "markdown"], default="text"
    )
    args = parser.parse_args()

    root = Path(args.root).resolve()
    checks_to_run = args.checks or list(ALL_CHECKS.keys())

    errors: list[str] = []
    warnings: list[str] = []
    infos: list[str] = []

    for name in checks_to_run:
        if name not in ALL_CHECKS:
            print(f"Unknown check: {name}", file=sys.stderr)
            continue
        results = ALL_CHECKS[name](root)
        for r in results:
            if r.startswith("[error]"):
                errors.append(r)
            elif r.startswith("[warning]"):
                warnings.append(r)
            else:
                infos.append(r)

    if args.format == "markdown":
        print("## Test Sync Validation Report\n")
        if errors:
            print(f"### Errors ({len(errors)})\n")
            for e in errors:
                print(f"- {e}")
            print()
        if warnings:
            print(f"### Warnings ({len(warnings)})\n")
            for w in warnings:
                print(f"- {w}")
            print()
        if infos:
            print(f"### Info ({len(infos)})\n")
            for i in infos:
                print(f"- {i}")
            print()
        if not errors and not warnings and not infos:
            print("All checks passed.\n")
    elif args.format == "json":
        import json
        print(json.dumps(
            {"errors": errors, "warnings": warnings, "infos": infos},
            indent=2
        ))
    else:
        for e in errors:
            print(e)
        for w in warnings:
            print(w)
        for i in infos:
            print(i)
        if not errors and not warnings and not infos:
            print(f"All {len(checks_to_run)} check(s) passed.")

    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
