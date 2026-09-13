using System.Collections.Concurrent;
using Vitarish.Modules.Base.Core.Platform.Transactions;

namespace Vitarish.Main.Infrastructure.Platform;

/// <summary>
/// Structured-logging backed implementation of IExternalApiTransactionLogger.
/// Logs all outbound API transactions as structured log events for observability.
/// </summary>
internal sealed class LoggingApiTransactionLogger(
    ILogger<LoggingApiTransactionLogger> logger,
    TimeProvider timeProvider) : IExternalApiTransactionLogger
{
    private readonly ConcurrentDictionary<Guid, PendingTransaction> _pending = new();

    public Task<Guid> BeginTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default)
    {
        transaction.Id = transaction.Id == Guid.Empty ? Guid.NewGuid() : transaction.Id;
        transaction.StartedAt = timeProvider.GetUtcNow().UtcDateTime;

        _ = _pending.TryAdd(transaction.Id, new(transaction, timeProvider.GetTimestamp()));

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
        bool found = _pending.TryRemove(transactionId, out PendingTransaction? pending);

        long durationMs = found ? ElapsedMilliseconds(pending!.StartTimestamp) : 0;
        if (found)
        {
            pending!.Transaction.DurationMs = durationMs;
            pending.Transaction.FinishedAt = timeProvider.GetUtcNow().UtcDateTime;
        }

        ApiTransaction? completed = pending?.Transaction;

        if (success)
        {
            logger.LogInformation(
                "API transaction {TransactionId} succeeded: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, durationMs={DurationMs}]",
                transactionId,
                completed?.ProviderKey ?? "?",
                completed?.OperationName ?? "?",
                completed?.CorrelationId ?? "?",
                responseCode,
                durationMs);
        }
        else
        {
            logger.LogWarning(
                "API transaction {TransactionId} failed: {ProviderKey}/{OperationName} " +
                "[correlationId={CorrelationId}, responseCode={ResponseCode}, errorCategory={ErrorCategory}, durationMs={DurationMs}]",
                transactionId,
                completed?.ProviderKey ?? "?",
                completed?.OperationName ?? "?",
                completed?.CorrelationId ?? "?",
                responseCode,
                errorCategory,
                durationMs);
        }

        return Task.CompletedTask;
    }

    public Task LogTransactionAsync(ApiTransaction transaction, CancellationToken cancellationToken = default)
    {
        long durationMs = transaction.DurationMs;

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

    private long ElapsedMilliseconds(long startTimestamp) =>
        Math.Max(0, (long)timeProvider.GetElapsedTime(startTimestamp).TotalMilliseconds);

    private sealed record PendingTransaction(ApiTransaction Transaction, long StartTimestamp);
}
