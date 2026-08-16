using System.Collections.Concurrent;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

namespace Vitarish.E2e.Infrastructure;

internal sealed class E2eInMemoryOperationReceiptStore<T> : IOperationReceiptStore<T>
{
    private readonly ConcurrentDictionary<OperationReceiptKey, OperationReceipt<T>> receipts = new();

    public Task<OperationReceipt<T>?> GetAsync(OperationReceiptKey key, CancellationToken cancellationToken) =>
        Task.FromResult(receipts.TryGetValue(key, out OperationReceipt<T>? receipt) ? receipt : null);

    public Task<bool> TryClaimAsync(OperationReceiptKey key, T value, TimeSpan lifetime, CancellationToken cancellationToken) =>
        Task.FromResult(receipts.TryAdd(key, new OperationReceipt<T>(0, value)));

    public Task<bool> TryUpdateAsync(OperationReceiptKey key, long expectedRevision, T value, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        while (receipts.TryGetValue(key, out OperationReceipt<T>? current) && current.Revision == expectedRevision)
        {
            if (receipts.TryUpdate(key, new OperationReceipt<T>(expectedRevision + 1, value), current))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
