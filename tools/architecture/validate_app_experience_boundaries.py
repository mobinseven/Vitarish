#!/usr/bin/env python3
"""Validate ADR-0008 app experience documentation boundaries."""

from __future__ import annotations

from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
DOCS_APPS = REPO_ROOT / "docs" / "apps"

COLLECTIONS = {
    "decision-trees",
    "journeys",
    "tests",
    "decisions",
    "roadmaps",
    "tasks",
    "runbooks",
}

# ADR-0008 keeps this as a temporary historical source until the scheduled
# content migration finishes. New app docs must not use modules/.
LEGACY_MODULE_PREFIXES = {
    Path("clinic-owner/modules"),
}


def normalized_relative(path: Path) -> str:
    return path.relative_to(REPO_ROOT).as_posix()


def is_allowed_legacy_module_path(app_relative: Path) -> bool:
    return any(app_relative == prefix or prefix in app_relative.parents for prefix in LEGACY_MODULE_PREFIXES)


def validate() -> list[str]:
    errors: list[str] = []

    if not DOCS_APPS.exists():
        return errors

    for file_path in DOCS_APPS.rglob("*"):
        if not file_path.is_file():
            continue

        app_relative = file_path.relative_to(DOCS_APPS)
        parts = app_relative.parts
        if len(parts) < 2:
            continue

        app_name = parts[0]
        collection = parts[1]

        if collection == "modules" and not is_allowed_legacy_module_path(app_relative):
            errors.append(
                f"{normalized_relative(file_path)} is under docs/apps/{app_name}/modules; "
                "ADR-0008 app docs must use flat app-owned collections."
            )

        if collection in COLLECTIONS and len(parts) > 3:
            errors.append(
                f"{normalized_relative(file_path)} is nested below docs/apps/{app_name}/{collection}; "
                "collection files must be flat."
            )

        if collection == "tests":
            text = file_path.read_text(encoding="utf-8", errors="replace")
            cites_journey = "journeys/" in text or "../journeys/" in text or "Related journey: N/A" in text
            if not cites_journey:
                errors.append(
                    f"{normalized_relative(file_path)} is an app test doc but does not cite an app journey or N/A."
                )

        if collection == "journeys":
            text = file_path.read_text(encoding="utf-8", errors="replace")
            if "Derives from:" in text and "decision-trees/" not in text:
                errors.append(
                    f"{normalized_relative(file_path)} says it derives from an upstream document "
                    "but does not link to an app decision tree."
                )

    return errors


def main() -> int:
    errors = validate()
    if errors:
        print("ADR-0008 app experience validation failed.")
        for error in errors:
            print(f"- {error}")
        return 1

    print("ADR-0008 app experience validation passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
