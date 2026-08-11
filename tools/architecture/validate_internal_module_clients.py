#!/usr/bin/env python3
"""Validate authenticated transport and registration for internal module clients."""
from __future__ import annotations

import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BASE_REGISTRATION = Path(
    "src/Modules/Base/Client/DependencyInjection/"
    "InternalModuleClientRegistrationExtensions.cs"
)
CLIENT_CLASS_RE = re.compile(
    r"\b(?:public|internal)\s+(?:(?:sealed|abstract|partial)\s+)*class\s+"
    r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
DIRECT_REGISTRATION_RE = re.compile(
    r"\.(?P<method>AddHttpClient|AddScoped|AddTransient|AddSingleton)\s*"
    r"<(?P<types>[^>]+)>"
)


@dataclass(frozen=True)
class Violation:
    path: Path
    line: int
    kind: str
    forbidden: str
    reason: str


def line_number(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def client_files() -> list[Path]:
    return sorted(
        (
            path.relative_to(ROOT)
            for path in ROOT.glob("src/Modules/*/Client/**/*.cs")
            if path.is_file()
            and not {"bin", "obj"}.intersection(path.relative_to(ROOT).parts)
            and "src/Modules/Base/Client/" not in path.as_posix()
        ),
        key=lambda path: path.as_posix(),
    )


def production_cs_files() -> list[Path]:
    return sorted(
        (
            path.relative_to(ROOT)
            for path in ROOT.glob("src/**/*.cs")
            if path.is_file()
            and not {"bin", "obj"}.intersection(path.relative_to(ROOT).parts)
        ),
        key=lambda path: path.as_posix(),
    )


def scan_client(path: Path, text: str) -> tuple[list[Violation], set[str]]:
    class_names = {match.group("name") for match in CLIENT_CLASS_RE.finditer(text)}
    if "HttpClient" not in text or "AuthenticatedModuleApiClient" in text:
        return [], class_names

    offset = text.find("HttpClient")
    return [
        Violation(
            path,
            line_number(text, offset),
            "UnauthenticatedModuleClient",
            "HttpClient",
            "Internal module clients must derive from AuthenticatedModuleApiClient.",
        )
    ], class_names


def scan_registration(
    path: Path, text: str, client_class_names: set[str]
) -> list[Violation]:
    if path == BASE_REGISTRATION:
        return []

    violations: list[Violation] = []
    for match in DIRECT_REGISTRATION_RE.finditer(text):
        registered_types = {
            token.strip().split(".")[-1]
            for token in match.group("types").split(",")
        }
        if not registered_types.intersection(client_class_names):
            continue

        violations.append(
            Violation(
                path,
                line_number(text, match.start()),
                "DirectModuleClientRegistration",
                match.group("method"),
                "Register internal module clients through "
                "AddInternalModuleApiClient<TClientContract,TClientImplementation>.",
            )
        )
    return violations


def self_test() -> None:
    unsafe_client = "public sealed class UnsafeClient(HttpClient client) : IUnsafeClient {}"
    violations, names = scan_client(Path("UnsafeClient.cs"), unsafe_client)
    assert len(violations) == 1
    assert names == {"UnsafeClient"}

    safe_client = (
        "public sealed class SafeClient(HttpClient client) "
        ": AuthenticatedModuleApiClient(client), ISafeClient {}"
    )
    violations, names = scan_client(Path("SafeClient.cs"), safe_client)
    assert not violations
    assert names == {"SafeClient"}

    direct_registration = "services.AddHttpClient<ISafeClient, SafeClient>();"
    violations = scan_registration(
        Path("Registration.cs"), direct_registration, {"SafeClient"}
    )
    assert len(violations) == 1
    print("Internal module client boundary parser self-test passed")


def main() -> int:
    if "--self-test" in sys.argv:
        self_test()
        return 0

    violations: list[Violation] = []
    client_class_names: set[str] = set()
    for path in client_files():
        text = (ROOT / path).read_text(errors="ignore")
        client_violations, names = scan_client(path, text)
        violations.extend(client_violations)
        client_class_names.update(names)

    for path in production_cs_files():
        text = (ROOT / path).read_text(errors="ignore")
        violations.extend(scan_registration(path, text, client_class_names))

    print("Internal module client boundary validation")
    print("Violations found:", len(violations))
    for violation in violations:
        print(
            f"{violation.path}:{violation.line}: kind={violation.kind}; "
            f"forbidden={violation.forbidden}; reason={violation.reason}"
        )
    return 1 if violations else 0


if __name__ == "__main__":
    raise SystemExit(main())
