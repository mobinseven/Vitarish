# Appointments Boundary Migration Test Plan

Owner: Platform Appointments module
Capability: ADR-0003 frontend/backend isolation and ADR-004 protected module API

## Required automated coverage

- Contract serialization round trips for appointment command requests, query requests, read DTOs, result/error DTOs, enums, `DateOnly`, `TimeOnly`, `TimeSpan`, and UTC temporary hold expiry instants.
- Client failure mapping for validation, unauthorized, forbidden, not found, conflict/stale state, timeout, cancellation, transport, deserialization, and unexpected failures.
- API mapping for contract-to-Application command/query adaptation, Application failures to structured contract errors, protected endpoint group usage, command metadata responses, and read-only query behavior.

## Hot scenarios

1. Simultaneous booking capacity conflict: run two authenticated users against the same tenant/shift capacity boundary and verify only permitted reservations succeed while losers receive conflict/stale-state errors.
2. Temporary hold expiry: create a hold, wait past expiry, then confirm it and verify confirmation is rejected without creating a reservation.
3. Reminder policy authorization: attempt create/update/delete from unauthorized, provider, and tenant-admin contexts and verify only authorized principals mutate policy state.
4. Tenant isolation: seed reservations, shifts, services, providers, booking policies, reminder policies, and holds in two tenants; verify cross-tenant reads and commands return not found/forbidden and do not mutate the other tenant.
