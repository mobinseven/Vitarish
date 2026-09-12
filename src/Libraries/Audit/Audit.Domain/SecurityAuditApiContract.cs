namespace Vitarish.Audit.Domain;

public static class SecurityAuditApiRoutes
{
    public const string DocumentName = "audit";
    public const string DocumentTag = "Audit";
    public const string Base = "/api/audit";
    public const string SecurityEvents = "/security-events";
}

public static class SecurityAuditApiOperations
{
    public const string ListSecurityEvents = "Audit_SecurityEvents_List";
}
