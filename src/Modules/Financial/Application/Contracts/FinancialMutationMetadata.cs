namespace Vitarish.Financial.Application.Contracts;

public sealed record FinancialMutationMetadata(
    string Actor,
    string Source,
    string CorrelationId,
    string CausationId,
    string? ProviderReference = null);
