# Vitarish Agent Map

This repository uses a modular .NET solution that separates client-facing APIs, hosting shells, and domain-driven modules. Use this file as a quick orientation guide when extending or reviewing the codebase.

## Top-level layout
- `src/`: Production code organized by concern.
  - `Clients/`: Client-facing HTTP endpoints and interfaces. Projects follow a layered structure (`Clients.*`) to expose module functionality.
  - `Hosts/`: Host applications that compose modules into runnable services.
  - `Interfaces/`: Interface projects that make up the UI users interact with.
  - `Identity/`: Identity domain bounded context. Subfolders (`ApiClients`, `AuthenticationClients`, `Users`, `Parties`, `Requests`) each contain `Domain`, `Application`, `Infrastructure`, and `Hosting` projects.
  - `Modules/`: Business modules (e.g., `Communications`, `Files`, `Instagram`, `Intelligence`) built with the same `Domain`/`Application`/`Infrastructure`/`Hosting` layering. `Modules/Base` provides shared primitives.
- `tests/`: Mirrors `src/` structure for automated tests, keeping test projects aligned with their production counterparts.
- `Vitarish.sln`: Solution file wiring the projects together with Debug/Release configurations.

## Layering conventions
- **Domain**: Entities, value objects, aggregates, and business rules.
- **Application**: Use cases, commands/queries, DTOs, and orchestration logic.
- **Infrastructure**: Persistence, external service integrations, and configuration.
- **Hosting**: Composition roots defining API hosts, DI setup, and runtime entry points.

## Working tips
- When adding functionality, align new code with the existing bounded context folder and project naming (e.g., `Module.Feature.Application`).
- Keep domain logic free of infrastructure dependencies; inject abstractions where needed.
- Add corresponding tests under `tests/` matching the `src/` path to maintain symmetry.

Use this map as a starting point; dive into the relevant module folder for deeper details and existing patterns.

## Cautions

- The `Identity/` shall not need nor reference any projects outside this folder.
- The projects under `Clients/` shall not under any circumstances reference any project out of this folder. If a client project needed any object from outside of this folder, a corresponding DTO must be created for the object inside that project.
- When creating UI components, consult `DSG.md` for design systems guidelines. If the component has a page under Mock hosts (e.g. Main.Mock) and take screenshots of the resulting design. If it doesn't have, add a page for the component and follow suit.

## Pull requests

Always check for a successful build and passing all tests before creating a PR.
