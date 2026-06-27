using System.Collections.Concurrent;
using Vitarish.Modules.Base.Core.Platform.Transactions;

namespace Vitarish.Main.Infrastructure.Platform;

/// <summary>
/// Structured-logging backed implementation of IExternalApiTransactionLogger.
/// Logs all outbound API transactions as structured log events for observability.
/// </summary>
internal sealed class LoggingApiTransactionLogger(ILogger<LoggingApiTransactionLogger> logger) : IExternalApiTransactionLogger
{
    private readonly ConcurrentDictionary<Guid, (ApiTransaction Transaction, DateTime StartedAt)> _pending = new();

    public Task<Guid> BeginTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default)
    {
        transaction.Id = transaction.Id == Guid.Empty ? Guid.NewGuid() : transaction.Id;
        transaction.StartedAt = DateTime.UtcNow;

        _ = _pending.TryAdd(transaction.Id, (transaction, transaction.StartedAt));

        logger.LogDebug(
            "API transaction {TransactionId} started: {ProviderKey}/{OperationName} [correlationId={CorrelationId}]",
            transaction.Id, transaction.ProviderKey, transaction.OperationName, transaction.CorrelationId);

        return Task.FromResult(transaction.Id);
    }

    public Task CompleteTransactionAsync(
        Guid transactionId,
        int? responseCode,
        string? responseMessage,
        bool success,
        string? errorCategory = null,
        CancellationToken cancellationToken = default)
    {
        _ = _pending.TryRemove(transactionId, out (ApiTransaction Transaction, DateTime StartedAt) pending);

        long durationMs = pending.StartedAt == default
            ? 0
            : (long)(DateTime.UtcNow - pending.StartedAt).TotalMilliseconds;

        if (success)
        {
            logger.LogInformation(
                "API transaction {TransactionId} succeeded: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, durationMs={DurationMs}]",
                transactionId,
                pending.Transaction?.ProviderKey ?? "?",
                pending.Transaction?.OperationName ?? "?",
                pending.Transaction?.CorrelationId ?? "?",
                responseCode,
                durationMs);
        }
        else
        {
            logger.LogWarning(
                "API transaction {TransactionId} failed: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, errorCategory={ErrorCategory}, durationMs={DurationMs}]",
                transactionId,
                pending.Transaction?.ProviderKey ?? "?",
                pending.Transaction?.OperationName ?? "?",
                pending.Transaction?.CorrelationId ?? "?",
                responseCode,
                errorCategory,
                durationMs);
        }

        return Task.CompletedTask;
    }

    public Task LogTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default)
    {
        long durationMs = transaction.FinishedAt.HasValue
            ? (long)(transaction.FinishedAt.Value - transaction.StartedAt).TotalMilliseconds
            : 0;

        if (transaction.Success)
        {
            logger.LogInformation(
                "API transaction {TransactionId} completed: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, durationMs={DurationMs}]",
                transaction.Id, transaction.ProviderKey, transaction.OperationName,
                transaction.CorrelationId, transaction.ResponseCode, durationMs);
        }
        else
        {
            logger.LogWarning(
                "API transaction {TransactionId} failed: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, errorCategory={ErrorCategory}, durationMs={DurationMs}]",
                transaction.Id, transaction.ProviderKey, transaction.OperationName,
                transaction.CorrelationId, transaction.ResponseCode, transaction.ErrorCategory, durationMs);
        }

        return Task.CompletedTask;
    }
}
