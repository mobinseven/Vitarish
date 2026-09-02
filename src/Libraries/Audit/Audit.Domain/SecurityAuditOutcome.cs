namespace Vitarish.Audit.Domain;

public static class SecurityAuditOutcome
{
    public const string Started = "started";
    public const string Succeeded = "succeeded";
    public const string Denied = "denied";
    public const string Failed = "failed";
    public const string Pending = "pending";
}
