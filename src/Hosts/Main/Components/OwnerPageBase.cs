using MudBlazor;
using Vitarish.Parties.Application.Abstractions;

namespace Vitarish.Main.Components;

/// <summary>
/// Base class for all owner management host pages.
/// Adds IPartiesService and ISnackbar to the base PageBase injections
/// and exposes a helper for resolving the current owner GUID from the request context.
/// </summary>
public class OwnerPageBase : PageBase
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Parses <see cref="Vitarish.Requests.Application.Abstractions.IRequestContext.OwnerId"/> to a Guid.
    /// Returns null when no owner claim is present or the value is not a valid Guid.
    /// </summary>
    protected Guid? GetOwnerGuid()
    {
        if (RequestContext.OwnerId is null)
            return null;

        return Guid.TryParse(RequestContext.OwnerId, out Guid g) ? g : null;
    }
}
