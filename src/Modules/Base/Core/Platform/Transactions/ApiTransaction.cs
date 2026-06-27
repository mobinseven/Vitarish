namespace Vitarish.Modules.Base.Core.Platform.Transactions;

/// <summary>
/// Records a single outbound API call to an external service.
/// Used for observability, debugging, and operational support.
/// </summary>
public sealed class ApiTransaction
{
    public Guid Id { get; set; }

    /// <summary>Identifier of the external provider (e.g., "TAMIN", "SALAMAT").</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Name of the operation performed (e.g., "CheckEntitlement", "SubmitPrescription").</summary>
    public required string OperationName { get; set; }

    /// <summary>Deterministic hash of the request payload for deduplication.</summary>
    public string? RequestHash { get; set; }

    /// <summary>End-to-end correlation identifier linking related operations.</summary>
    public required string CorrelationId { get; set; }

    /// <summary>Unique request identifier for this specific call.</summary>
    public string? RequestId { get; set; }

    /// <summary>Idempotency key sent to the provider (if applicable).</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>HTTP status code or provider-native response code.</summary>
    public int? ResponseCode { get; set; }

    /// <summary>Response message summary (sensitive data masked).</summary>
    public string? ResponseMessage { get; set; }

    /// <summary>Duration of the API call in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>When the call started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>When the call completed (or timed out).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Whether the call completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Canonical error category if the call failed.</summary>
    public string? ErrorCategory { get; set; }

    /// <summary>Tenant context for multi-tenant isolation.</summary>
    public string TenantId { get; set; } = string.Empty;
}
