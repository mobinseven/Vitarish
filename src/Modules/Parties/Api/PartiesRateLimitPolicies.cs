namespace Vitarish.Parties.Api;

public static class PartiesRateLimitPolicies
{
    /// <summary>Self-service tenant creation per client IP; the per-creator limit is durable in Application.</summary>
    public const string SelfServiceTenantCreation = "parties-self-service-tenant-creation";
}
