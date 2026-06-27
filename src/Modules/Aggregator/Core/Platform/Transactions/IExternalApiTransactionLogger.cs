namespace Vitarish.Modules.Aggregator.Core.Platform.Transactions;

/// <summary>
/// Centralized service for logging outbound API calls to external providers.
/// Any module integrating with external services should use this interface
/// for consistent observability and debugging.
/// </summary>
public interface IExternalApiTransactionLogger
{
    /// <summary>
    /// Records the start of an outbound API call. Returns a transaction ID
    /// that should be used to record the completion.
    /// </summary>
    Task<Guid> BeginTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the completion of a previously started API call.
    /// </summary>
    Task CompleteTransactionAsync(
        Guid transactionId,
        int? responseCode,
        string? responseMessage,
        bool success,
        string? errorCategory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a complete API transaction in a single call (for cases where
    /// begin/end pattern is not needed).
    /// </summary>
    Task LogTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default);
}
