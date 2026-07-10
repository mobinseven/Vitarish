# ADR-0003 Layer Boundary Validation

ADR-0003 Contracts/Client/API boundary rules remain active. ADR-0009 retired Blazor/Razor Interface projects, so there is no longer an Interface boundary configuration file or report-only migration mode.

Temporary exceptions for reintroducing Interface projects are not permitted unless a future accepted ADR supersedes ADR-0009.

## Local and CI command

```bash
python3 tools/architecture/validate_no_blazor.py
python3 tools/architecture/validate_module_api_boundaries.py
```

The same commands are executed by `.github/workflows/architecture-validation.yml` and should be run before focused or full repository builds.

## Final fail-fast policy

The no-Blazor validator fails for:

- any production Razor component artifact;
- `Microsoft.NET.Sdk.Razor` or MudBlazor references;
- Blazor runtime registration patterns;
- `src/Modules/*/Interface`, `src/Apps/*/Interface`, or `src/Interfaces/*`.

The module API boundary validator fails protected internal module APIs and clients that bypass the approved endpoint/client boundary.

## Final done criteria

- No production Interface project exists.
- Every migrated `<Module>.Client` uses HTTP and contract DTOs.
- Every migrated `<Module>.Api` maps HTTP contracts to Application commands/queries without business rules.
- Contracts are the serialization source of truth for wire DTOs, routes, and public API enums.
- Client and API failure handling is explicit.
- Static checks validate the boundary.
- React Web remains the only supported production UI surface unless a future accepted ADR changes the UI strategy.
