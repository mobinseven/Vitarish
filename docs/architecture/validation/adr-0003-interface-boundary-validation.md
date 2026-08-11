# ADR-0003 Boundary Validation

ADR-0003 Contracts/Client/API boundary rules remain active. ADR-0009 retired Blazor/Razor Interface projects, so there is no longer an Interface boundary configuration file or report-only migration mode.

Temporary exceptions for reintroducing Interface projects are not permitted unless a future accepted ADR supersedes ADR-0009.

## Current validation

There is no repository-wide static validator for the complete ADR-0003 layer
and project-reference matrix. The former module API boundary script was removed
because it did not validate that matrix and had no current CI caller.

For project or layer-boundary changes:

1. Review changed project references against the dependency matrix in
   `docs/architecture.md`.
2. Verify that `rg --files src/Modules -g '*.Interface.csproj'` returns no
   production Interface project.
3. Run the affected module tests.
4. Run the merge-level backend build from `docs/codebase-map.md`.

Adjacent endpoint and internal-client transport checks remain automated:

```bash
python3 tools/architecture/validate_openapi_endpoint_coverage.py
python3 tools/architecture/validate_internal_module_clients.py
```

These scripts do not prove the complete ADR-0003 dependency matrix and must not
be reported as doing so.

## Final done criteria

- No production Interface project exists.
- Every migrated `<Module>.Client` uses HTTP and contract DTOs.
- Every migrated `<Module>.Api` maps HTTP contracts to Application commands/queries without business rules.
- Contracts are the serialization source of truth for wire DTOs, routes, and public API enums.
- Client and API failure handling is explicit.
- Applicable static checks, focused tests, project-reference review, and the
  merge-level backend build validate the changed boundary.
- React Web remains the only supported production UI surface unless a future accepted ADR changes the UI strategy.
