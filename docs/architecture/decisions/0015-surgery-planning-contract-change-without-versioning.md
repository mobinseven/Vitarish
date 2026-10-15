# ADR-0015: Surgery Planning Contract Change Without Versioning

Date: 2026-10-15
Status: Accepted
Scope: Clinic surgery and patient HTTP contracts changed by surgery planning (#869)
Owner: Clinic owner
Supersedes:
Superseded by:
Related: issue #871, PR #869

## Context

Surgery planning (#869) changed Clinic compatibility boundaries that root
`AGENTS.md` (Compatibility, Migrations) says need explicit versioning or a
migration:

- `SurgeryCatalogDto.PersonsToNotify` was renamed to `DefaultTeamRoles`.
- The `/dashboard/surgeries` route and the `ClinicListDashboardSurgeries`
  operation ID were removed.
- `DateOfSurgery` was removed from the patient read and write contracts and from
  the assistant patient overview and facts. The `DateOfSurgery` sort value stays
  as an alias for the next surgery.

Patients may also have a stored `DateOfSurgery` value in their metadata. After
the #869 review fix, saving a patient no longer deletes it, but nothing shows or
migrates it.

## Decision

1. The owner confirmed that no caller outside this repository uses these Clinic
   endpoints. The in-repo Web clients were regenerated in the same change, so the
   renamed and removed members need no versioned or deprecated path.
2. Stored `DateOfSurgery` metadata values stay as they are: kept, not shown, not
   migrated, and not deleted.

## Consequences

- No compatibility window or aliases to maintain for these members.
- An outside caller added later must use the current contract.
- Stored `DateOfSurgery` values remain in patient metadata with no reader. They
  are not lost, and can still be migrated into surgery records or removed by a
  later, explicit migration.

## Alternatives Considered

### Option 1: Record the exemption (chosen)

No outside callers exist, so versioning would add cost without protecting anyone.

### Option 2: Deprecated aliases

Keep the old field, route and operation ID for a compatibility window. Rejected:
there is no caller to keep working.

### Option 3: Migrate or remove the stored dates

Turn stored dates into completed surgery records, show them read-only, or delete
them with a recorded migration. Deferred by the owner; the values stay untouched.

## Validation / Enforcement

- Any new outside caller of the Clinic HTTP API requires versioning for further
  breaking changes, per root `AGENTS.md`.
- A change that reads, migrates or deletes stored `DateOfSurgery` metadata must
  reference this ADR and state its data effect.

## Notes

The open `DateOfSurgery` values are tracked in `docs/memory.md` under
"Durable decisions to remember".
