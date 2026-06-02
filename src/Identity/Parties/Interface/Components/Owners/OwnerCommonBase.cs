using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Parties.Interface.Components.Owners;

/// <summary>
/// Base class for all owner-facing interface components.
/// Injects the parties service and snackbar so every component can query/mutate owner data and surface feedback.
/// </summary>
public class OwnerCommonBase : CommonBase
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Parses <see cref="Requests.Application.Abstractions.IRequestContext.OwnerId"/> to a nullable Guid.
    /// Returns null when the claim is absent or not a valid Guid.
    /// </summary>
    protected Guid? GetOwnerGuid() => RequestContext.OwnerId is null ? null : Guid.TryParse(RequestContext.OwnerId, out Guid g) ? g : null;
}
