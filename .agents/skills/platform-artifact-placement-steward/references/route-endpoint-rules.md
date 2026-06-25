# Route and Endpoint Rules

## Module API Route Prefix

Module API routes use this prefix:

```txt
/api/modules/<module>
```

The `<module>` segment is lowercase kebab-case and identifies the module route ownership.

## Route Artifact Placement

- Host UI navigation routes: `src/Hosts/<Host>/Common/Routes`
- Module API route constants shared by Client and Api: `src/Modules/<Module>/Contracts`
- Endpoint registration methods such as `Map<Module>Api`: `src/Modules/<Module>/Api`
- Host startup calls to endpoint registration: host or module composition root according to existing convention

## Endpoint Mapping Expectations

- Use explicit endpoint registration methods such as `Map<Module>Api`.
- Keep endpoint handlers thin.
- Map contract DTOs to Application commands or queries at the API boundary.
- Propagate `CancellationToken` from the HTTP request through Application calls.
- Return structured failures.
- Avoid domain/entity leakage in responses.
- Avoid UI/client dependencies in API projects.

## Command and Query Expectations

- Command endpoints mutate state and must not act as data query APIs.
- Query endpoints return data and must not mutate observable state.
- Combined command/query behavior requires redesign before implementation.
