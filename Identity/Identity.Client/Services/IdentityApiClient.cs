using System.Net.Http.Json;
using Vitarish.Identity.Contracts.Requests;

namespace Vitarish.Identity.Client.Services;

/// <summary>
/// Client for interacting with the Identity API.
/// </summary>
public class IdentityApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured with the Identity API base URL.</param>
    public IdentityApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets an owner by ID.
    /// </summary>
    public async Task<OwnerResponse?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<OwnerResponse>($"api/owners/{id}", ct);
    }

    /// <summary>
    /// Gets all owners.
    /// </summary>
    public async Task<IEnumerable<OwnerResponse>?> GetOwnersAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<OwnerResponse>>("api/owners", ct);
    }

    /// <summary>
    /// Creates a new owner.
    /// </summary>
    public async Task<OwnerResponse?> CreateOwnerAsync(CreateOwnerRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/owners", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OwnerResponse>(ct);
    }

    /// <summary>
    /// Updates an existing owner.
    /// </summary>
    public async Task<OwnerResponse?> UpdateOwnerAsync(UpdateOwnerRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/owners/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OwnerResponse>(ct);
    }

    /// <summary>
    /// Deletes an owner by ID.
    /// </summary>
    public async Task DeleteOwnerAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/owners/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    public async Task<TenantResponse?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<TenantResponse>($"api/tenants/{id}", ct);
    }

    /// <summary>
    /// Gets all tenants.
    /// </summary>
    public async Task<IEnumerable<TenantResponse>?> GetTenantsAsync(CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<TenantResponse>>("api/tenants", ct);
    }

    /// <summary>
    /// Gets tenants by owner ID.
    /// </summary>
    public async Task<IEnumerable<TenantResponse>?> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<TenantResponse>>($"api/owners/{ownerId}/tenants", ct);
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    public async Task<TenantResponse?> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tenants", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantResponse>(ct);
    }

    /// <summary>
    /// Updates an existing tenant.
    /// </summary>
    public async Task<TenantResponse?> UpdateTenantAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tenants/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantResponse>(ct);
    }

    /// <summary>
    /// Deletes a tenant by ID.
    /// </summary>
    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/tenants/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}

/// <summary>
/// Response DTO for Owner.
/// </summary>
public sealed record OwnerResponse(
    Guid Id,
    string Name,
    string? LogoUrl,
    string? Description,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>
/// Response DTO for Tenant.
/// </summary>
public sealed record TenantResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string? LogoUrl,
    string? Description,
    string ClientId,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);
