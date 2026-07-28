namespace Vitarish.Modules.Base.Core.Platform.Idempotency;

public readonly record struct OperationReceiptKey(
    string Scope,
    string TenantId,
    string IdempotencyKey);

public sealed record OperationReceipt<T>(long Revision, T Value);

public interface IOperationReceiptStore<T>
{
    Task<OperationReceipt<T>?> GetAsync(
        OperationReceiptKey key,
        CancellationToken cancellationToken);

    Task<bool> TryClaimAsync(
        OperationReceiptKey key,
        T value,
        TimeSpan lifetime,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateAsync(
        OperationReceiptKey key,
        long expectedRevision,
        T value,
        TimeSpan lifetime,
        CancellationToken cancellationToken);
}
