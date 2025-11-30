
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;

namespace Vorn.Aaas.Parties.Infrastructure.Extensions;
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

    /// <summary>
    /// Maps a TenantIndicator to AuthenticationClient properties.
    /// Updates all fields on the provided client instance.
    /// </summary>
    public static void MapToClient(TenantIndicator indicator, Tenant tenant, AuthenticationClient client)
    {
        client.Url = indicator.Value;
        client.Title = indicator.IsPrimary
            ? $"{tenant.Name} (primary)"
            : $"{tenant.Name} ({indicator.Method.ToString().ToLowerInvariant()})";
        client.Description = $"Tenant={tenant.Id} Indicator={indicator.Method}:{indicator.Value} Status={indicator.Status}";
        client.LogoUrl = tenant.LogoUrl;
        // TODO: AutoAccess should be moved to tenant-level configuration (see issue spec)
        // Currently tied to Active status as interim solution
        client.AutoAccess = indicator.Status == IndicatorStatus.Active;
    }

    /// <summary>
    /// Creates a new AuthenticationClient from a TenantIndicator.
    /// </summary>
    public static AuthenticationClient CreateClient(TenantIndicator indicator, Tenant tenant)
    {
        string clientId = GenerateClientId(indicator.TenantId, indicator.Method, indicator.Value);
        AuthenticationClient client = new()
        {
            Id = clientId,
            Url = indicator.Value
        };
        MapToClient(indicator, tenant, client);
        return client;
    }
}
