# Financial Module

## Operational workflows

### Posting
- Submit posting through `IFinancialLedgerCommandService.PostAsync` with idempotency key.
- Entry is created as draft, validated for balanced lines, and posted atomically.
- Only posted entries affect account balance projections.

### Reversal and adjustment
- Posted entries are immutable.
- Corrections are done by creating new reversing or adjusting posted entries.
- Original posted entries are never edited.

### Reconciliation
- Mismatches are recorded through `RecordMismatchAsync` with mismatch state and authority.
- Reconciliation repair uses new financial mutations (typically adjustment templates), not history edits.

### Replay and backfill authority
- Idempotency is command-scoped (`financial:{scope}` + key).
- Same key with same request hash replays prior response.
- Same key with different request hash returns conflict.
- Adapter ingestion stores both raw provider payload and normalized payload, deduped by provider+dedupe key.
