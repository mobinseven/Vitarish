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

## React Web Host Validation

Run the React Web host’s own package-manager scripts when reviewing or changing files under a React/TypeScript host. Use the package manager implied by the host lockfile and the scripts declared in that host’s `package.json`.

Prefer existing scripts in this order when present:

```bash
npm run lint
npm run typecheck
npm test
npm run build
```

If the host uses `pnpm`, `yarn`, or another package manager, use the equivalent commands from that host. Do not invent scripts that are not declared.

For generated OpenAPI/Orval output, also run the host’s generation or contract script when it exists. Generated output under `src/api/generated` must be reproducible and manually unedited.

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

Include React Web host validation impact when reviewing changes under:

- `src/Hosts/*/src/**/*.ts`
- `src/Hosts/*/src/**/*.tsx`
- `src/Hosts/*/src/**/*.js`
- `src/Hosts/*/src/**/*.jsx`
- `src/Hosts/*/package.json`
- `src/Hosts/*/vite.config.*`
- `src/Hosts/*/tsconfig*.json`
- `src/Hosts/*/src/api/generated`
- `src/Hosts/*/src/pages`
- `src/Hosts/*/src/features`
- `src/Hosts/*/src/components`
- `src/Hosts/*/src/ui`
