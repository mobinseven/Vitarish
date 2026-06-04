namespace Vitarish.Financial.Application.Contracts;

public sealed record PostLedgerEntryResponse(Guid EntryId, LedgerEntryState State, DateTime? PostedAt, string IdempotencyKey);
