using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Components;

/// <summary>
/// Base class for all owner management host pages.
/// Adds IPartiesService and ISnackbar to the base PageBase injections
/// and exposes helpers for the common owner/tenant loading pattern.
/// </summary>
public class OwnerPageBase : PageBase
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Parses <see cref="Vitarish.Requests.Application.Abstractions.IRequestContext.OwnerId"/> to a Guid.
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

        TenantReadDto? tenant = await PartiesService.GetTenantAsync(tenantId.Value);
        if (tenant is null)
        {
            NavigationManager.NavigateTo(fallbackRoute);
        }

        return tenant;
    }
}
