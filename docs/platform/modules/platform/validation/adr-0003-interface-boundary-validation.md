# ADR-0003 Interface Boundary Validation

ADR-003 validation is globally fail-fast. `tools/architecture/interface_boundary_config.json` uses `defaultMode: FailFast`, and no module may remain configured as `ReportOnly` after the completed rollout.

Temporary exceptions are removed and are not permitted for the completed rollout unless a future ADR or explicit architecture exception reintroduces them with a removal condition.

## Local and CI command

```bash
python3 tools/architecture/validate_interface_boundaries.py
```

## Final done criteria

- Every migrated `<Module>.Interface` references only `<Module>.Contracts`, approved frontend-safe UI/base libraries, and explicitly approved frontend-safe cross-module contracts/interfaces.
- Every migrated `<Module>.Client` uses HTTP and contract DTOs.
- Every migrated `<Module>.Api` maps HTTP contracts to Application commands/queries without business rules.
- Contracts are the serialization source of truth for wire DTOs, routes, and public API enums.
- Client and API failure handling is explicit.
- Static checks validate the boundary.
- No module Interface project references backend Application, Domain, Infrastructure, Api, or `Entities.Server`.
