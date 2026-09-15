namespace Vitarish.Audit.Domain;

public static class SecurityAuditApiRoutes
{
    public const string DocumentName = "audit";
    public const string DocumentTag = "Audit";
    public const string Base = "/api/audit";
    public const string SecurityEvents = "/security-events";
    public const string PlatformSecurityEvents = "/platform/security-events";
    public const string PlatformSecurityEventCatalog = "/platform/security-event-catalog";
    public const string PlatformSecurityEventsExport = "/platform/security-events/export";
    public const string CsvContentType = "text/csv; charset=utf-8";
    public const string NoStoreCacheControl = "no-store";
}

public static class SecurityAuditApiOperations
{
    public const string ListSecurityEvents = "Audit_SecurityEvents_List";
    public const string ListPlatformSecurityEvents = "Audit_PlatformSecurityEvents_List";
    public const string GetPlatformSecurityEventCatalog = "Audit_PlatformSecurityEventCatalog_Get";
    public const string ExportPlatformSecurityEvents = "Audit_PlatformSecurityEvents_Export";
}
