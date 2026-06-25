# Test, Documentation, and ADR Routing

## ADR Impact Rules

Recommend an ADR when placement introduces or changes a stable architecture rule, such as:

- a new module boundary
- a new app boundary
- a new interface package boundary
- a new shared platform primitive
- a new library capability boundary
- a new cross-module dependency direction
- a new frontend/backend isolation pattern
- a new persistence strategy
- a new host composition pattern
- a new identity boundary
- a new rule that future implementations must follow

Do not recommend an ADR for ordinary placement that already follows the placement table, an accepted ADR, and existing repository convention.

ADR-0003 already decides the module `Contracts` / `Client` / `Api` / `Interface` isolation pattern. Do not request a new ADR merely to apply ADR-0003 to a migrated module. Recommend ADR review only when a module needs to deviate from ADR-0003 or add a new stable platform rule.

## Test Placement Rules

Suggest corresponding test placement using the existing test project convention first:

- Module tests: `tests/Modules/<Module>.Tests/...`
- Module layer subfolders, when useful inside the test project: `Application`, `Domain`, `Infrastructure`, `Interface`, `Contracts`, `Client`, `Api`, `TestHelpers`
- Host tests: `tests/Hosts/<Host>.Tests/...`
- Interface package tests: `tests/Interfaces/Interfaces.<Name>.Tests/...`
- App-specific tests: `tests/Apps/<App>/...` when an app test convention exists, otherwise call out the missing convention
- Identity tests: `tests/Identity/<BoundedContext>/<Layer>/...` or the existing bounded-context test layout
- Library tests: `tests/Libraries/<Library>/<Library>.<Project>.Tests/...` or the established library convention
- Platform/base tests: `tests/Modules/Base.Tests/...` or the established platform convention
- ADR-0003 boundary validation: `tools/architecture/validate_interface_boundaries.py`
- Hot-test scenario documentation: use the Test Curator when verification depends on runtime infrastructure, browser flow, external services, DB behavior, queues, files, clocks, or network timing

Do not create a new test project layout when an existing test project for the scope already exists.

## Documentation Placement Rules

Documentation should follow scoped ownership and existing sources of truth:

- App/module docs: `docs/apps/<app>/modules/<module>/...`
- Platform module docs: `docs/platform/modules/<module>/...`
- Platform conventions: `docs/platform/modules/platform/conventions/...`
- Architecture decisions: existing ADR convention, including `docs/architecture/decisions` or scoped `docs/platform/modules/<module>/decisions`
- ADR-0003 decision: `docs/platform/modules/platform/decisions/0003-isolate-module-interface-libraries-with-api-client-contracts.md`
- ADR-0003 implementation conventions: `docs/platform/modules/platform/conventions/adr-0003-implementation-conventions.md`
- ADR-0003 validation guidance: `docs/platform/modules/platform/validation/adr-0003-interface-boundary-validation.md`
- Developer guidance: `docs/development/...` or existing `docs/platform/developer-guides/...` convention
- Operations/runbooks: `docs/runbooks/...` or existing `docs/platform/operations/...` convention
- Journey, roadmap, test, and decision docs: place under the owning scoped docs tree when one exists

Do not duplicate the same rule in multiple places. Use indexes and links for navigation.

If an established documentation source of truth already exists, update or reference it instead of creating a competing location.
