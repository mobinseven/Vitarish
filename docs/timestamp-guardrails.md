# Timestamp persistence guardrails

Use this review checklist for timestamp changes:

1. Persisted entities and EF-owned models should use UTC `DateTime` for instants and map them to PostgreSQL `timestamp with time zone`.
2. Do not add `DateTimeOffset` to persisted entity properties unless the original offset or time zone is a business requirement. Document that requirement next to the model or in an ADR.
3. External contracts and provider metadata may use `DateTimeOffset` when the offset belongs to the protocol, SDK, cookie, certificate, or object-storage response.
4. Local-only concepts should be modeled explicitly with `DateOnly`, `TimeOnly`, or a local `DateTime` plus a business `TimeZoneId`/`OffsetMinutes` when conversion to an instant is required.
5. At application boundaries, normalize persisted instants: keep DateTimeKind.Utc values as-is and treat unspecified DateTime values as UTC (or convert local DateTime values with ToUniversalTime()). Convert external DateTimeOffset values with .UtcDateTime before assigning to persisted entities.

Suggested static review command:

```bash
rg -n "DateTimeOffset" src tests --glob '!**/Migrations/**' --glob '!bin/**' --glob '!obj/**'
```

Remaining matches should be classified as external contracts, provider metadata, cookies/certificates, or explicitly offset-sensitive values.
