# Sea ADR-003 Boundary Migration Test Plan

Date: 2026-06-27
Status: Suggested
App: Main
Module: Sea
Capability: Frontend/backend boundary correction
Owner: Platform
Related ADRs: Sea ADR-003; Platform ADR-0003
Confidence: Medium

## Required boundary checks

- Add a build/static assertion that `src/Modules/Sea/Interface/Sea.Interface.csproj` references only Sea `Client` and `Contracts` for backend Sea access and never references Sea `Application`, `Domain`, `Infrastructure`, or `Api`.
- Add a source dependency check over `src/Modules/Sea/Interface` and `src/Hosts/Main/Pages/Sea` that fails on imports of `Vitarish.Sea.Application`, `Vitarish.Sea.Domain`, `Vitarish.Sea.Infrastructure`, or `Vitarish.Sea.Api`.

## Required client contract tests

- Verify each `ISeaClient` query uses the expected route, HTTP method, success deserialization, and structured error mapping.
- Verify each `ISeaClient` command returns `SeaCommandResult` without refreshing read DTOs implicitly.
- Cover cancellation, timeout, transport failure, and deserialization failure.

## Required API mapping tests

- Cover representative project, procedure, event, attachment, role, user-role, event-role, receipt, statistics, and snapshot endpoints.
- Verify API endpoints bind contract shapes only and map to Application service inputs before returning contract responses.
- Verify explicit status mapping for validation, unauthorized/forbidden, not found, conflict, timeout/cancellation, and unexpected failures.

## Suggested component tests

- Component tests for failed `SeaResult` handling, snackbar/error display, and loading-state transitions in migrated project/procedure/event pages.
- Component tests for role assignment and attachment creation flows where UI behavior depends on multiple client calls.

## Hot scenarios

- Browser smoke test for Sea project → procedure → event navigation with read receipt marking.
- Auth/authorization scenario for forbidden event writes and role-gated event visibility.
- File attachment scenario with upload metadata creation and attachment deletion.
