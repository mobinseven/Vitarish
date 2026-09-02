namespace Vitarish.Audit.Application;

/// <summary>
/// Completes a previously recorded pending security-audit operation by
/// appending a terminal event. The original pending event remains immutable.
/// </summary>
public interface ISecurityAuditOutcomeUpdater
{
    Task<string?> GetOutcomeAsync(
        string tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SecurityAuditWriteResult> UpdateAsync(
        string tenantId,
        string idempotencyKey,
        string outcome,
        string? reason,
        CancellationToken cancellationToken);
}
