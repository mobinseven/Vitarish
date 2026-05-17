namespace Vitarish.Financial.Application.Contracts;

public sealed record PostLedgerEntryResponse(Guid EntryId, LedgerEntryState State, DateTimeOffset? PostedAt, string IdempotencyKey);
