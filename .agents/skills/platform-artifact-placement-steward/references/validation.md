# Validation Rules

## ADR-0003 Validator

Run this when reviewing migrated module Interface changes:

```bash
python3 tools/architecture/validate_interface_boundaries.py
```

The validator configuration is:

```txt
tools/architecture/interface_boundary_config.json
```

It scans:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`

It reports forbidden backend dependencies from module Interface projects and source files, including:

- `ProjectReference` violations in `.csproj` files
- `CSharpNamespaceImport` violations in `.cs` files
- `CSharpFullyQualifiedUsage` violations in `.cs` files
- `RazorNamespaceImport` violations in `.razor` files
- `RazorFullyQualifiedUsage` violations in `.razor` files

Recommend this validation command unless the user explicitly asked for static discussion only.

## Rollout Modes

- `ReportOnly`: violations are actionable but do not fail the command.
- `FailFast`: violations cause a non-zero exit code.

Temporary exceptions must be recorded in the validator configuration with the path, forbidden dependency or namespace, reason, and planned removal condition.

## Validation Impact Triggers

Include ADR-0003 boundary validation impact when reviewing changes under:

- `src/Modules/*/Interface/*.csproj`
- `src/Modules/*/Interface/**/*.cs`
- `src/Modules/*/Interface/**/*.razor`
- `src/Modules/*/Contracts`
- `src/Modules/*/Client`
- `src/Modules/*/Api`
