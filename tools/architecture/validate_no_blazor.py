#!/usr/bin/env python3
"""Validate ADR-0009: no Blazor/Razor Component UI or Interface projects.

Fails on any tracked production occurrence of:
- *.razor, *.razor.cs, *.razor.css, *.razor.js
- Microsoft.NET.Sdk.Razor in .csproj
- MudBlazor package references
- AddRazorComponents, MapRazorComponents, InteractiveServer, blazor.web.js
- src/Modules/*/Interface, src/Apps/*/Interface, src/Interfaces/*
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

RAZOR_FILE_EXTENSIONS = {".razor", ".razor.cs", ".razor.css", ".razor.js"}
RAZOR_SDK = "Microsoft.NET.Sdk.Razor"
MUDBLAZOR = "MudBlazor"
FORBIDDEN_CODE_PATTERNS = [
    re.compile(r"AddRazorComponents"),
    re.compile(r"MapRazorComponents"),
    re.compile(r"InteractiveServer"),
    re.compile(r"blazor\.web\.js"),
]
FORBIDDEN_DIR_PATTERNS = [
    re.compile(r"src/Modules/[^/]+/Interface/"),
    re.compile(r"src/Apps/[^/]+/Interface/"),
    re.compile(r"src/Interfaces/"),
]
SOURCE_EXTENSIONS = {".cs", ".csproj", ".props", ".targets", ".json", ".yml", ".yaml", ".md", ".txt"}


def repository_root() -> Path:
    return Path(__file__).resolve().parents[2]


def relative_to(root: Path, path: Path) -> str:
    try:
        return path.resolve().relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def scan_razor_files(root: Path) -> list[str]:
    violations: list[str] = []
    src = root / "src"
    if not src.exists():
        return violations

    for ext in RAZOR_FILE_EXTENSIONS:
        for path in src.rglob(f"*{ext}"):
            rel = path.relative_to(root).as_posix()
            if "/bin/" in rel or "/obj/" in rel:
                continue
            violations.append(f"Razor file found: {rel}")

    return violations


def scan_forbidden_content(root: Path) -> list[str]:
    violations: list[str] = []
    src = root / "src"
    if not src.exists():
        return violations

    for path in src.rglob("*"):
        if path.is_dir():
            continue
        if "/bin/" in path.as_posix() or "/obj/" in path.as_posix():
            continue
        if path.suffix not in SOURCE_EXTENSIONS and path.suffix not in {".razor", ".razor.cs"}:
            continue

        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue

        rel = relative_to(root, path)

        if path.suffix == ".csproj" or path.suffix == ".props":
            if RAZOR_SDK in text:
                violations.append(f"Razor SDK found in {rel}")
            if MUDBLAZOR in text:
                violations.append(f"MudBlazor reference found in {rel}")

        for pattern in FORBIDDEN_CODE_PATTERNS:
            for match in pattern.finditer(text):
                line = text.count("\n", 0, match.start()) + 1
                violations.append(f"Forbidden pattern '{match.group()}' in {rel}:{line}")

    return violations


def scan_forbidden_directories(root: Path) -> list[str]:
    violations: list[str] = []
    src = root / "src"
    if not src.exists():
        return violations

    for path in src.rglob("*.csproj"):
        rel = relative_to(root, path)
        for pattern in FORBIDDEN_DIR_PATTERNS:
            if pattern.search(rel):
                violations.append(f"Interface project found: {rel}")

    return violations


def main() -> int:
    root = repository_root()

    violations: list[str] = []
    violations.extend(scan_razor_files(root))
    violations.extend(scan_forbidden_content(root))
    violations.extend(scan_forbidden_directories(root))

    print("ADR-0009 no-Blazor validation")
    print(f"Violations found: {len(violations)}")

    for violation in violations:
        print(f"  {violation}")

    return 1 if violations else 0


if __name__ == "__main__":
    sys.exit(main())
