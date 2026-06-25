# Dependency and Conflict Rules

## General Dependency Rules

Prefer inward, stable dependencies:

- Domain must stay pure and should not depend on Application, Contracts, Client, Api, Infrastructure, Interface, Host, App runtime composition, or concrete technology packages.
- Application may depend on Domain and application-owned abstractions/contracts.
- Contracts must stay serialization-safe and must not contain behavior or backend implementation dependencies.
- Client may depend on Contracts and frontend-safe HTTP abstractions.
- Api may depend on Contracts and Application and must remain a thin HTTP adapter.
- Infrastructure may depend on Application contracts and Domain to implement concrete adapters.
- Module Interface artifacts may depend on Contracts, Client, UI-facing models/services, and approved frontend-safe libraries according to ADR-0003 and module convention.
- `src/Interfaces/Interfaces.*` packages must remain UI-facing and must not depend on backend implementation details, persistence, host pages, or infrastructure adapters.
- Host pages compose route-level UI and should not own reusable module or interface package behavior.
- Apps own app-specific behavior and should not become a dumping ground for reusable module logic.
- Identity artifacts stay under Identity bounded contexts.
- Shared libraries and `src/Modules/Base` must not contain module-specific business behavior.
- Tests mirror or reference the production scope they verify.
- Documentation follows the existing scoped source of truth and should not duplicate rules.

## Conflict Detection

Report a possible placement conflict when:

- domain code depends on infrastructure, UI, host, app-specific behavior, vendor SDKs, clocks, filesystems, network clients, or persistence packages
- domain code depends on Contracts, Client, or Api
- application code depends on infrastructure implementation rather than application-owned abstractions
- application code depends on Interface, Client, Api, HTTP, UI, EF/database details, vendor SDKs, filesystems, clocks, or network dependencies outside abstractions
- Contracts contain domain entities, persistence models, business rules, EF attributes, application handlers, repositories, infrastructure adapters, UI state, or server abstractions
- Client code depends on Application, Domain, Infrastructure, Api, server projects, persistence libraries, or backend-only cross-module projects
- Client methods hide failures, omit cancellation tokens, or expose raw domain/entity state
- Api endpoint handlers contain business rules, return domain entities directly, depend on Interface/Client/UI, or leak persistence details
- module Interface code depends on Application, Domain, Infrastructure, Api, backend implementations, persistence packages, or `Entities.Server`
- Razor/UI code constructs raw backend routes for migrated workflows when a module client exists
- host pages contain reusable module or interface package UI logic
- `src/Interfaces/Interfaces.*` packages depend on backend implementation, persistence, host pages, or infrastructure adapters
- shared libraries or `src/Modules/Base` contain module-specific business behavior
- commands return query models while also changing state without an explicit, narrow reason
- queries change observable state
- repositories own multiple unrelated persistence concerns
- app-specific artifacts leak into reusable modules
- identity-specific behavior is placed under ordinary modules
- tests are detached from the production scope they verify
- documentation creates a competing source of truth
- localization resources are split across multiple owners for the same UI text
- an artifact named `Manager`, `Helper`, `Util`, `Common`, or vague `Service` hides multiple responsibilities

Use “possible conflict” when evidence is incomplete.
