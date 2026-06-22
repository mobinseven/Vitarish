# ADR: Persist instants as UTC DateTime mapped to PostgreSQL timestamp with time zone

## Status

Accepted

## Context

Persisted timestamps represent instants in time, not the caller's original offset. Keeping offsets for audit and operational timestamps created multiple representations of the same fact and required persistence workarounds for PostgreSQL.

## Decision

Persist application instants as `DateTime` values whose `Kind` is UTC. Entity audit timestamps and other persisted instant fields must map to PostgreSQL `timestamp with time zone`.

Use these temporal types consistently:

- Use UTC `DateTime` for persisted instants such as audit fields, event occurrence times, retry times, expirations, and submitted/processed timestamps.
- Use `DateOnly` for calendar dates with no clock time, such as appointment dates or business-effective dates.
- Use `TimeOnly` for clock times without a date, such as daily shift start and end times.
- Use local `DateTime` only for local business date/time concepts that are not instants until a business time zone is applied.
- Add `TimeZoneId` or `OffsetMinutes` when the original zone or offset is a business requirement and must be preserved.
- Keep `DateTimeOffset` at external protocol or provider-metadata boundaries when the provider contract includes an offset, but convert to UTC `DateTime` before persistence.

## Guardrail

Persisted entities and EF-owned models must not expose `DateTimeOffset` properties unless the property has an explicit business justification documenting why offset or zone preservation is required. Reviewers should reject unclassified `DateTimeOffset` in domain entity and DbContext-owned model types.

External contracts, cookies, cryptographic certificate validity, object-storage metadata, and SDK DTOs may continue to use `DateTimeOffset` when they mirror provider semantics and are not persisted as PostgreSQL instants.

## Consequences

- The platform has one source of truth for persisted instants: UTC `DateTime`.
- EF mappings no longer need a broad `DateTimeOffset` persistence workaround.
- Original offsets for audit and operational timestamps are intentionally not preserved.
