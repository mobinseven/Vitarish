namespace Vitarish.Financial.Domain.Enums;

public enum ReconciliationMismatchState
{
    None = 0,
    MissingExternal = 1,
    MissingInternal = 2,
    AmountMismatch = 3,
    CurrencyMismatch = 4,
    Resolved = 5
}
