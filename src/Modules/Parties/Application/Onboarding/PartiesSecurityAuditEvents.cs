using Vitarish.Audit.Domain;

namespace Vitarish.Parties.Application.Onboarding;

public static class PartiesSecurityAuditEvents
{
    public const string SourceBoundary = "parties.application";
    public const string TenantResourcePrefix = "tenant/";
    public const string SelfServiceTenantCreation = SecurityAuditEventCatalog.SelfServiceTenantCreationEvent;
    public const string SelfServiceTenantCreationOperation = SecurityAuditEventCatalog.SelfServiceTenantCreationOperation;
    public const string SelfServiceTenantCreationIdempotencySuffix = ":self-service-tenant-creation";
}
