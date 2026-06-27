#!/usr/bin/env python3
"""Validate ADR-0003 layer dependency boundaries.

This validator is intentionally fail-fast: configuration drift, temporary
exceptions, report-only mode, omitted migrated projects, and boundary violations
all return a non-zero exit code.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

MODES = {"FailFast", "ReportOnly"}
FRONTEND_SCOPES = {"Interface", "Client", "Contracts", "GlobalInterface"}
LAYERS = {"Interface", "Client", "Contracts", "Api", "Application", "Domain", "Infrastructure"}
BACKEND_LAYERS = {"Application", "Domain", "Infrastructure", "Api"}
SERVER_ONLY_PROJECT_FRAGMENTS = ("Libraries/Entities/Entities.Server",)
SERVER_ONLY_NAMESPACE_FRAGMENTS = ("Vitarish.Entities.Server", "Entities.Server")
SOURCE_EXTENSIONS = {".cs", ".razor"}

MODULE_PROJECT_RE = re.compile(r"^src/Modules/([^/]+)/([^/]+)/[^/]+\.csproj$")


@dataclass(frozen=True)
class ProjectScope:
    name: str
    layer: str
    project: Path
    root: Path
    module: str | None = None


@dataclass(frozen=True)
class Violation:
    scope: str
    mode: str
    kind: str
    path: Path
    line: int
    forbidden: str
    reason: str


def repository_root() -> Path:
    return Path(__file__).resolve().parents[2]


def relative_to(root: Path, path: Path) -> str:
    try:
        return path.resolve().relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, max(offset, 0)) + 1


def load_config(root: Path, path: Path) -> dict:
    if not path.exists():
        raise FileNotFoundError(f"configuration file not found: {relative_to(root, path)}")

    config = json.loads(path.read_text(encoding="utf-8"))

    default_mode = config.get("defaultMode")
    if default_mode != "FailFast":
        raise ValueError("defaultMode must be FailFast")

    for module_name, settings in config.get("modules", {}).items():
        mode = settings.get("mode", default_mode)
        if mode != "FailFast":
            raise ValueError(f"module {module_name} must be FailFast")
        if settings.get("temporaryExceptions"):
            raise ValueError(f"module {module_name} temporaryExceptions must be empty")

    for group_name, settings in config.get("globalInterfaces", {}).items():
        mode = settings.get("mode", default_mode)
        if mode != "FailFast":
            raise ValueError(f"global Interface group {group_name} must be FailFast")
        if settings.get("temporaryExceptions"):
            raise ValueError(f"global Interface group {group_name} temporaryExceptions must be empty")

    if config.get("temporaryExceptions"):
        raise ValueError("top-level temporaryExceptions must be empty")

    return config


def discover_projects(root: Path) -> list[ProjectScope]:
    scopes: list[ProjectScope] = []

    for project in sorted(root.glob("src/Modules/*/*/*.csproj")):
        parts = project.relative_to(root).parts
        if len(parts) < 5:
            continue

        module = parts[2]
        layer = parts[3]
        if layer not in LAYERS:
            continue

        scopes.append(
            ProjectScope(
                name=f"{module}.{layer}",
                layer=layer,
                project=project,
                root=project.parent,
                module=module,
            )
        )

    for project in sorted(root.glob("src/Interfaces/*/*.csproj")):
        name = project.parent.name
        scopes.append(
            ProjectScope(
                name=name,
                layer="GlobalInterface",
                project=project,
                root=project.parent,
                module=None,
            )
        )

    return scopes


def ensure_coverage(root: Path, config: dict, scopes: list[ProjectScope]) -> list[Violation]:
    violations: list[Violation] = []
    config_path = Path("tools/architecture/interface_boundary_config.json")

    discovered_modules = sorted(
        {scope.module for scope in scopes if scope.module and scope.layer == "Interface"}
    )
    configured_modules = sorted(config.get("modules", {}))

    for module in sorted(set(discovered_modules) - set(configured_modules)):
        violations.append(
            Violation(
                scope=module,
                mode="FailFast",
                kind="ConfigurationCoverage",
                path=config_path,
                line=1,
                forbidden=module,
                reason="Migrated module Interface is omitted from validation config.",
            )
        )

    for module in sorted(set(configured_modules) - set(discovered_modules)):
        violations.append(
            Violation(
                scope=module,
                mode="FailFast",
                kind="ConfigurationCoverage",
                path=config_path,
                line=1,
                forbidden=module,
                reason="Configured module Interface project was not discovered.",
            )
        )

    discovered_globals = sorted(scope.name for scope in scopes if scope.layer == "GlobalInterface")
    configured_globals = sorted(config.get("globalInterfaces", {}))

    for group in sorted(set(discovered_globals) - set(configured_globals)):
        violations.append(
            Violation(
                scope=group,
                mode="FailFast",
                kind="ConfigurationCoverage",
                path=config_path,
                line=1,
                forbidden=group,
                reason="Global frontend Interface group is omitted from validation config.",
            )
        )

    for group in sorted(set(configured_globals) - set(discovered_globals)):
        violations.append(
            Violation(
                scope=group,
                mode="FailFast",
                kind="ConfigurationCoverage",
                path=config_path,
                line=1,
                forbidden=group,
                reason="Configured global Interface group was not discovered.",
            )
        )

    return violations


def project_reference_path(project: Path, include: str) -> Path:
    return (project.parent / include.replace("\\", "/")).resolve()


def is_project_allowlisted(config: dict, relative_project_path: str) -> bool:
    return any(
        relative_project_path.endswith(str(entry.get("project", "")))
        for entry in config.get("frontendSafeDependencyAllowlist", [])
    )


def is_cross_module_frontend_safe_allowed(
    config: dict,
    source_module: str | None,
    target_project: str,
) -> bool:
    if not source_module:
        return False

    return any(
        entry.get("sourceModule") == source_module and entry.get("targetProject") == target_project
        for entry in config.get("crossModuleFrontendSafeAllowlist", [])
    )


def module_project_parts(relative_project_path: str) -> tuple[str, str] | None:
    match = MODULE_PROJECT_RE.match(relative_project_path)
    if not match:
        return None

    return match.group(1), match.group(2)


def classify_project_reference(
    root: Path,
    scope: ProjectScope,
    target: Path,
    config: dict,
) -> tuple[str, str] | None:
    relative_target = relative_to(root, target)
    target_parts = module_project_parts(relative_target)

    # Server-only libraries are never frontend-safe, even if a broad allowlist is present.
    if scope.layer in FRONTEND_SCOPES and any(
        fragment in relative_target for fragment in SERVER_ONLY_PROJECT_FRAGMENTS
    ):
        return "Entities.Server", "This layer must not reference server-only Entities.Server."

    if scope.layer in FRONTEND_SCOPES:
        if is_project_allowlisted(config, relative_target):
            return None

        if not target_parts:
            return None

        target_module, target_layer = target_parts

        if scope.layer in {"Interface", "GlobalInterface"}:
            if target_layer in BACKEND_LAYERS:
                return (
                    f"{target_module}.{target_layer}",
                    "Frontend Interface projects must not reference backend projects.",
                )

            if target_layer == "Client":
                same_module_client = scope.module is not None and target_module == scope.module
                explicitly_allowed = is_cross_module_frontend_safe_allowed(
                    config,
                    scope.module,
                    relative_target,
                )

                if not same_module_client and not explicitly_allowed:
                    return (
                        f"{target_module}.Client",
                        "Interface projects may only reference same-module Client projects or explicitly allowed cross-module Client projects.",
                    )

        if scope.layer == "Client" and target_layer != "Contracts":
            return (
                f"{target_module}.{target_layer}",
                "Client projects may reference only Contracts plus approved frontend-safe base libraries.",
            )

        if scope.layer == "Contracts" and target_layer != "Contracts":
            return (
                f"{target_module}.{target_layer}",
                "Contracts projects may not reference module implementation layers.",
            )

        return None

    if scope.layer == "Api" and target_parts:
        target_module, target_layer = target_parts
        if target_layer in {"Interface", "Client", "Infrastructure", "Domain"}:
            return (
                f"{target_module}.{target_layer}",
                "Api projects must adapt Contracts to Application without Interface, Client, Infrastructure, or direct Domain dependencies.",
            )

    return None


def scan_project_references(root: Path, scope: ProjectScope, config: dict) -> list[Violation]:
    text = scope.project.read_text(encoding="utf-8")
    violations: list[Violation] = []

    try:
        xml = ET.fromstring(text)
    except ET.ParseError as error:
        line = getattr(error, "position", (1, 0))[0]
        return [
            Violation(
                scope=scope.name,
                mode="FailFast",
                kind="ProjectXml",
                path=scope.project.relative_to(root),
                line=line,
                forbidden=scope.name,
                reason="Project XML could not be parsed.",
            )
        ]

    for item in xml.iter():
        if item.tag.split("}")[-1] != "ProjectReference":
            continue

        include = item.attrib.get("Include")
        if not include:
            continue

        classified = classify_project_reference(
            root,
            scope,
            project_reference_path(scope.project, include),
            config,
        )

        if classified:
            forbidden, reason = classified
            violations.append(
                Violation(
                    scope=scope.name,
                    mode="FailFast",
                    kind="ProjectReference",
                    path=scope.project.relative_to(root),
                    line=line_number(text, text.find(include)),
                    forbidden=forbidden,
                    reason=reason,
                )
            )

    return violations


def forbidden_namespace_patterns(scopes: list[ProjectScope]) -> list[tuple[re.Pattern[str], str, str]]:
    modules = sorted({scope.module for scope in scopes if scope.module})
    patterns: list[tuple[re.Pattern[str], str, str]] = []

    for module in modules:
        for layer in ("Application", "Domain", "Infrastructure", "Api"):
            tokens = (
                rf"Vitarish\.{module}\.{layer}",
                rf"Vitarish\.Modules\.{module}\.{layer}",
                rf"{module}\.{layer}",
            )

            for token in tokens:
                patterns.append(
                    (
                        re.compile(rf"(?<![A-Za-z0-9_]){token}(?:\b|\.)"),
                        f"{module}.{layer}",
                        "Frontend-safe source must not use backend namespaces.",
                    )
                )

    for namespace in SERVER_ONLY_NAMESPACE_FRAGMENTS:
        patterns.append(
            (
                re.compile(rf"(?<![A-Za-z0-9_]){re.escape(namespace)}(?:\b|\.)"),
                "Entities.Server",
                "Frontend-safe source must not use server-only Entities namespaces.",
            )
        )

    return patterns


def source_files(scope: ProjectScope) -> Iterable[Path]:
    for extension in sorted(SOURCE_EXTENSIONS):
        for path in scope.root.rglob(f"*{extension}"):
            relative_parts = path.relative_to(scope.root).parts
            if "bin" in relative_parts or "obj" in relative_parts:
                continue
            yield path


def scan_source_namespaces(
    root: Path,
    scope: ProjectScope,
    patterns: list[tuple[re.Pattern[str], str, str]],
) -> list[Violation]:
    if scope.layer not in FRONTEND_SCOPES:
        return []

    violations: list[Violation] = []

    for path in sorted(source_files(scope)):
        text = path.read_text(encoding="utf-8", errors="ignore")
        seen: set[tuple[int, str]] = set()

        for pattern, forbidden, reason in patterns:
            for match in pattern.finditer(text):
                line = line_number(text, match.start())
                key = (line, forbidden)
                if key in seen:
                    continue

                seen.add(key)
                violations.append(
                    Violation(
                        scope=scope.name,
                        mode="FailFast",
                        kind="NamespaceUsage",
                        path=path.relative_to(root),
                        line=line,
                        forbidden=forbidden,
                        reason=reason,
                    )
                )

    return violations


def collect_violations(root: Path, config: dict) -> tuple[list[ProjectScope], list[Violation]]:
    scopes = discover_projects(root)
    patterns = forbidden_namespace_patterns(scopes)

    violations = ensure_coverage(root, config, scopes)
    for scope in scopes:
        violations.extend(scan_project_references(root, scope, config))
        violations.extend(scan_source_namespaces(root, scope, patterns))

    return scopes, violations


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate ADR-0003 layer boundaries.")
    parser.add_argument(
        "--config",
        default="tools/architecture/interface_boundary_config.json",
        help="Path to interface boundary configuration, relative to the repository root.",
    )
    args = parser.parse_args()

    root = repository_root()

    try:
        config = load_config(root, root / args.config)
    except Exception as error:
        print(f"Configuration error: {error}")
        return 1

    scopes, violations = collect_violations(root, config)

    module_interfaces = sorted(
        {scope.module for scope in scopes if scope.layer == "Interface" and scope.module}
    )
    global_interfaces = sorted(scope.name for scope in scopes if scope.layer == "GlobalInterface")

    print("ADR-0003 layer boundary validation")
    print(f"Configuration: {args.config}")
    print("Module Interface groups: " + ", ".join(module_interfaces))
    print("Global Interface groups: " + ", ".join(global_interfaces))
    print(f"Violations found: {len(violations)}")

    for violation in violations:
        print(
            f"{violation.mode}: {violation.kind}: "
            f"{violation.path}:{violation.line}: "
            f"scope={violation.scope}; "
            f"forbidden={violation.forbidden}; "
            f"reason={violation.reason}"
        )

    return 1 if violations else 0


if __name__ == "__main__":
    sys.exit(main())
