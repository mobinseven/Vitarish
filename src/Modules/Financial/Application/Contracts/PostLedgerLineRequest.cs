namespace Vitarish.Financial.Application.Contracts;

public sealed record PostLedgerLineRequest(Guid AccountId, LedgerEntrySide Side, long AmountMinorUnits);
