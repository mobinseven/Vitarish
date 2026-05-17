namespace Vitarish.Financial.Application.Contracts;

public sealed record ReconciliationMismatchCommand(
    string Provider,
    string ExternalReference,
    ReconciliationAuthority Authority,
    ReconciliationMismatchState MismatchState,
    string Currency,
    int Exponent,
    long? InternalMinorUnits,
    long? ExternalMinorUnits,
    Guid? RelatedEntryId = null);
