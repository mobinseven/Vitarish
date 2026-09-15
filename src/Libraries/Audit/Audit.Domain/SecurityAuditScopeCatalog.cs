namespace Vitarish.Audit.Domain;

public static class SecurityAuditScopeCatalog
{
    public const string PlatformScopeId = "platform";
    public const string PlatformResource = "platform/security-audit";
    public const string PlatformReviewIdempotencySuffix = ":platform-audit-review";
    public const string PlatformExportIdempotencySuffix = ":platform-audit-export";
}
