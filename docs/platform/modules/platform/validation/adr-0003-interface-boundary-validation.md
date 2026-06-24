# ADR-0003 Interface boundary validation

Date: 2026-06-24
Status: Active
App: Platform
Module: Platform
Capability: Module frontend/backend isolation validation
Owner: Architecture
Related ADRs: `docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`

## Purpose

The ADR-0003 validator is a static repository check that reports forbidden backend dependencies from module `Interface` projects and source files.

It scans:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`

## Local command

Run the same command locally and in CI:

```bash
python3 tools/architecture/validate_interface_boundaries.py
```

The validator uses committed configuration from:

```txt
tools/architecture/interface_boundary_config.json
```

## Rollout mode

The initial rollout is per-module `ReportOnly` because existing modules still contain known ADR-0003 boundary violations. Report-only modules print actionable violations and exit successfully.

Migrated modules can be switched individually to `FailFast` in `tools/architecture/interface_boundary_config.json`. In `FailFast`, any violation for that module causes a non-zero exit code.

Temporary exceptions may be recorded per module in the configuration. Each exception must include the path, forbidden dependency or namespace, reason, and planned removal condition.

## Reported violation categories

The validator distinguishes:

- `ProjectReference` violations in `.csproj` files.
- `CSharpNamespaceImport` violations in `.cs` files.
- `CSharpFullyQualifiedUsage` violations in `.cs` files.
- `RazorNamespaceImport` violations in `.razor` files.
- `RazorFullyQualifiedUsage` violations in `.razor` files.

Each violation includes the module name, file path, line number, forbidden dependency or namespace, enforcement mode, and reason.

## Boundaries enforced

The validator has a single source of truth for forbidden layers in `tools/architecture/validate_interface_boundaries.py` and forbids module Interface code from referencing backend `Application`, `Domain`, `Infrastructure`, or `Api` layers, plus server-only `Entities.Server` dependencies.
