using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Identity.Internal;

internal sealed class TenantsService(
    IIdentityClientService aaasClientFactory,
    IIdentityClientContext clientIdentityContext,
    ILogger<TenantsService> logger) : ITenantsService
{
    public async Task<IEnumerable<TenantInfo>> GetAllForClientAsync(string? clientId = null, CancellationToken ct = default)
    {
        clientId ??= clientIdentityContext.ClientId;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Client ID is not available in the current context.");
        }

        try
        {
            HttpClient client = await aaasClientFactory.CreateClient("/api/tenants/").ConfigureAwait(false);

            using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(30));
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            using HttpResponseMessage response = await client.GetAsync($"by-client/{Uri.EscapeDataString(clientId)}", linkedCts.Token).ConfigureAwait(false);

            _ = response.EnsureSuccessStatusCode();

            IEnumerable<TenantApiDto>? tenants = await response.Content.ReadFromJsonAsync<IEnumerable<TenantApiDto>>(linkedCts.Token).ConfigureAwait(false);

            if (tenants is null)
            {
                logger.LogWarning("Failed to deserialize tenants for client {ClientId}", clientId);
                return [];
            }

            logger.LogInformation("Retrieved {Count} tenants for client {ClientId}", tenants.Count(), clientId);

            return tenants.Select(MapToTenantInfo).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to retrieve tenants from API for client {ClientId}", clientId);
            throw new InvalidOperationException($"Failed to retrieve tenants for client: {clientId}", ex);
        }
    }

    private static TenantInfo MapToTenantInfo(TenantApiDto tenant)
    {
        IndicatorDto? primaryIndicator = tenant.Indicators?.FirstOrDefault(i => i.IsPrimary);
        return new TenantInfo
        {
            TenantId = tenant.Id,
            OwnerId = tenant.OwnerId,
            CanonicalKey = ToCanonicalKey(tenant.Name),
            PrimaryHostname = primaryIndicator?.Value ?? string.Empty,
            Title = tenant.Name,
            LogoUrl = tenant.LogoUrl,
            AuthClientId = tenant.ClientId,
            Indicators = tenant.Indicators?
                .Where(i => i.Status == ActiveIndicatorStatus)
                .Select(i => new TenantIndicatorInfo
                {
                    Method = i.Method.ToString().ToLowerInvariant(),
                    Value = i.Value,
                    Status = "active"
                })
                .ToList() ?? []
        };
    }

    private static string ToCanonicalKey(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

    private const int ActiveIndicatorStatus = 3; // IndicatorStatus.Active

    private sealed record TenantApiDto
    {
        public Guid Id { get; init; }
        public Guid OwnerId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? LogoUrl { get; init; }
        public string ClientId { get; init; } = string.Empty;
        public List<IndicatorDto>? Indicators { get; init; }
    }

    private sealed record IndicatorDto
    {
        public int Method { get; init; }
        public string Value { get; init; } = string.Empty;
        public int Status { get; init; }
        public bool IsPrimary { get; init; }
    }
}
