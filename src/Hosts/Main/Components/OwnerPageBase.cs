using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Components;

/// <summary>
/// Base class for all owner management host pages.
/// Adds IPartiesService and ISnackbar to the base PageBase injections
/// and exposes helpers for the common owner/tenant loading pattern.
/// </summary>
public class OwnerPageBase : PageBase
{
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Parses <see cref="request context owner id"/> to a Guid.
    /// Returns null when no owner claim is present or the value is not a valid Guid.
    /// </summary>
    protected Guid? GetOwnerGuid() => RequestContext.OwnerId is null ? null : Guid.TryParse(RequestContext.OwnerId, out Guid g) ? g : null;

    /// <summary>
    /// Loads the tenant with <paramref name="tenantId"/> from the parties service.
    /// Navigates to <paramref name="fallbackRoute"/> and returns null when the id is null or the tenant does not exist.
    /// </summary>
    protected async Task<TenantReadDto?> LoadTenantOrRedirectAsync(Guid? tenantId, string fallbackRoute)
    {
        if (tenantId is null)
        {
            NavigationManager.NavigateTo(fallbackRoute);
            return null;
        }

        PartiesResult<TenantReadDto> tenantResult = await PartiesClient.GetTenantAsync(tenantId.Value);
        TenantReadDto? tenant = tenantResult.Value;
        if (!tenantResult.IsSuccess || tenant is null)
        {
            NavigationManager.NavigateTo(fallbackRoute);
        }

        return tenant;
    }
}
