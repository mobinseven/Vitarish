# Example: Procedure Status Placement Review

## Artifact Placement Plan

| Artifact | Classification | Owning scope | Preferred location | Related tests | ADR impact |
|---|---|---|---|---|---|
| `ProcedureStatus.cs` | Domain enum | Medical or Clinic module | `src/Modules/<Module>/Domain/Enums/ProcedureStatus.cs` | `tests/Modules/<Module>/Domain.Tests/...` | Not required if workflow states follow existing module rules |
| `ProcedureCancellationReason.cs` | Domain enum/value model | Medical or Clinic module | `src/Modules/<Module>/Domain/...` | Domain tests for valid states and transitions | Not required unless cancellation changes platform procedure semantics |
| `CancelProcedureCommand.cs` | Application command | Owning module | `src/Modules/<Module>/Application/Commands/CancelProcedure/` | Application command handler tests | Recommended if replacing delete semantics with cancel semantics changes accepted architecture |
| `ProcedureStatusBadge.razor` | Reusable module UI | Owning module | `src/Modules/<Module>/Interface/Components/ProcedureStatusBadge.razor` | Interface/component tests or UI hot scenario | Not required |
| `PatientProcedurePage.razor` | Routable host page | Main host | `src/Hosts/Main/Pages/...` | Host/UI flow or hot scenario | Not required |

## Placement Notes

Procedure status belongs to the module domain because it represents module-owned procedure state.

The host page may show status, but it must not own the reusable status behavior.

## Risks / Conflicts

Possible conflict if procedure status is stored only as UI metadata without a domain rule.

Possible conflict if route-level page code owns reusable timeline/status components.

## Recommended Next Step

Classify the owning module first, then add the domain state and application command before changing UI behavior.
