# ADR-0009: Decommission Blazor/Razor Component UI and Interface Projects

Date: 2026-07-10
Status: Accepted
Scope: Platform
Owner: Architecture
Supersedes: ADR-0003 (Interface UI allowance only; Contracts/Client/API boundary rules preserved)
Superseded by:
Related: blazor-decommission-plan.md

## Context

The platform currently maintains ~413 Razor files across 23 Interface projects and the Main host Blazor Server runtime. React Web is the established and actively maintained UI surface. The Blazor UI layer creates significant maintenance burden with no active production consumers — Blazor-only workflows (Platform admin, owner tenant management, Commerce, Medical/prescriptions, Sea owner/user/admin, Knowledge owner/admin, Communications admin/reminders) have been retired as product decisions.

ADR-0003 introduced Interface projects to isolate frontend from backend, but the Interface projects were built on Blazor/MudBlazor and are not consumed by the React Web host. The Contracts, Client, and API boundary rules from ADR-0003 remain valid and are preserved.

## Decision

1. Blazor/Razor Component UI is forbidden for new production code.
2. All 23 Interface projects under `src/Modules/*/Interface` and `src/Apps/*/Interface` are deleted.
3. The Blazor Server runtime in `src/Hosts/Main` is removed — Main becomes backend-only (API, Identity, Hangfire, OpenAPI, module initialization).
4. React Web is the sole UI surface for current and future app experiences.
5. Old Blazor URLs are removed without redirects — retired paths return normal 404/not-found behavior.
6. No React parity is required for retired Blazor-only workflows.
7. Backend APIs, Domain/Application behavior, and module infrastructure are preserved unless a separate module decision retires the capability itself.
8. C# Client projects remain only when a non-Blazor .NET consumer uses them; otherwise they are deleted.

### Ownership after decommission

| Layer | Owner |
|-------|-------|
| UI and app experiences | React Web feature packages |
| Backend behavior | Domain / Application / Infrastructure / Api / Contracts |
| HTTP client adapters | Client projects (only if surviving .NET consumer exists) |
| Transport contracts | Contracts projects |

## Consequences

Positive:

- Eliminates ~413 Razor files, 132 code-behind files, and 23 Interface projects from the maintenance surface.
- Removes MudBlazor dependency chain from the .NET solution.
- Simplifies Main host to a pure backend service.
- Aligns documentation with actual production UI surface.

Negative:

- Blazor-only workflows are permanently retired with no migration path.
- Teams familiar with the Blazor codebase lose that reference material.
- Any future .NET UI consumer would need a new approach (not Blazor).

## Alternatives Considered

### Option 1: Decommission (chosen)

Delete Blazor UI and Interface projects entirely. React Web is the sole UI. Lowest long-term maintenance cost.

### Option 2: Convert Interface projects to pure API client libraries

Rejected. The Interface projects contain UI components, view models, Blazor localization, MudBlazor wrappers, and plugin UI — not transport-safe client code. The existing Client projects already serve the API client role. Converting would create redundant, confusing abstractions.

### Option 3: Keep Blazor alongside React

Rejected. Maintaining two UI frameworks doubles the surface for bugs, design drift, and developer cognitive load with no production justification.

### Option 4: Do nothing

Rejected. The Blazor layer continues to grow stale, confuse contributors, and inflate the build/test matrix.

## Validation / Enforcement

A no-Blazor architecture validator will be added to CI that fails on:

- `*.razor`, `*.razor.cs`, `*.razor.css`, `*.razor.js` in production source
- `Microsoft.NET.Sdk.Razor` in any production `.csproj`
- `MudBlazor` package references
- `AddRazorComponents`, `MapRazorComponents`, `InteractiveServer`, `blazor.web.js`
- `src/Modules/*/Interface`, `src/Apps/*/Interface`, `src/Interfaces/*`

## Notes

- This ADR preserves ADR-0003's Contracts, Client, and API boundary rules. Only the Interface UI allowance is superseded.
- The retirement register for Blazor-only workflows is maintained in `blazor-decommission-plan.md`.
