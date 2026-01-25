using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Infrastructure.Extensions;
/// <summary>
/// Service for mapping TenantIndicator to AuthenticationClient fields.
/// Centralizes mapping logic to ensure consistency across event handlers and resync operations.
/// </summary>
internal static class AuthClientMappingExtensions
{
    /// <summary>
    /// Generates the deterministic client ID for a tenant indicator.
    /// </summary>
    public static string GenerateClientId(Guid tenantId, IndicatorMethod method, string value)
        => AuthClientIdGenerator.Generate(tenantId, method, value);
}
