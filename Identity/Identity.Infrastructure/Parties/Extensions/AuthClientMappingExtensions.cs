using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Infrastructure.Parties.Extensions;

/// <summary>
/// Service for mapping TenantIndicator to AuthenticationClient fields.
/// Centralizes mapping logic to ensure consistency across event handlers and resync operations.
/// </summary>
internal static class AuthClientMappingExtensions
{
    public static string GenerateClientId(Guid tenantId, IndicatorMethod method, string value)
        => AuthClientIdGenerator.Generate(tenantId, method, value);

    public static void MapToClient(TenantIndicator indicator, Tenant tenant, AuthenticationClient client)
    {
        string title = indicator.IsPrimary
            ? $"{tenant.Name} (primary)"
            : $"{tenant.Name} ({indicator.Method.ToString().ToLowerInvariant()})";

        string description = $"Tenant={tenant.Id} Indicator={indicator.Method}:{indicator.Value} Status={indicator.Status}";
        client.Update(indicator.Value, title, description, tenant.LogoUrl, indicator.Status == IndicatorStatus.Active);
    }

    public static AuthenticationClient CreateClient(TenantIndicator indicator, Tenant tenant)
    {
        string clientId = GenerateClientId(indicator.TenantId, indicator.Method, indicator.Value);
        string title = indicator.IsPrimary
            ? $"{tenant.Name} (primary)"
            : $"{tenant.Name} ({indicator.Method.ToString().ToLowerInvariant()})";

        string description = $"Tenant={tenant.Id} Indicator={indicator.Method}:{indicator.Value} Status={indicator.Status}";

        return new AuthenticationClient(
            clientId,
            indicator.Value,
            title,
            description,
            tenant.LogoUrl,
            indicator.Status == IndicatorStatus.Active);
    }
}
