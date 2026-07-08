# ADR-0003 Layer Boundary Validation

ADR-003 validation is globally fail-fast. `tools/architecture/interface_boundary_config.json` uses `defaultMode: FailFast`, and no module or global frontend Interface group may remain configured as `ReportOnly`.

Temporary exceptions are not permitted for the completed rollout. The validator treats any non-empty `temporaryExceptions` collection as a configuration error before scanning source code.

## Local and CI command

```bash
python3 tools/architecture/validate_interface_boundaries.py
```

The same command is executed by `.github/workflows/architecture-validation.yml` and should be run before focused or full repository builds.

## Final fail-fast policy

The validator scans:

- `src/Modules/*/Interface`
- `src/Modules/*/Client`
- `src/Modules/*/Contracts`
- `src/Modules/*/Api`
- `src/Modules/*/Application`
- `src/Modules/*/Domain`
- global frontend UI projects under `src/Interfaces/*`

Validation fails for:

- any ADR-003 boundary violation in fail-fast scope;
- any `ReportOnly` mode;
- any temporary exception;
- any migrated module Interface project omitted from configuration;
- any global frontend Interface group omitted from configuration;
- frontend-safe layers referencing backend Application, Domain, Infrastructure, Api, server-only libraries, or `Entities.Server`;
- Client projects referencing non-Contract module layers;
- Contracts projects referencing implementation, UI, HTTP client, or server-only layers;
- Api projects referencing Interface, Client, Infrastructure, or direct Domain projects.

## Final done criteria

- Every migrated `<Module>.Interface` references only `<Module>.Contracts`, `<Module>.Client`, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module contracts/interfaces.
- Every migrated `<Module>.Client` uses HTTP and contract DTOs.
- Every migrated `<Module>.Api` maps HTTP contracts to Application commands/queries without business rules.
- Contracts are the serialization source of truth for wire DTOs, routes, and public API enums.
- Client and API failure handling is explicit.
- Static checks validate the boundary.
- No module Interface project references backend Application, Domain, Infrastructure, Api, or `Entities.Server`.
