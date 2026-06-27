using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Modules.Base.Interface;

namespace Vitarish.Parties.Interface.Components.Base;

/// <summary>
/// Base class for all owner-facing interface components.
/// Injects the parties client and snackbar so every component can request owner data through HTTP APIs and surface feedback.
/// </summary>
public class PartiesCommonBase : TenantCommonBase
{
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;

    /// <summary>
    /// Parses the owner id from the ambient request context to a nullable Guid.
    /// Returns null when the claim is absent or not a valid Guid.
    /// </summary>
    protected Guid? GetOwnerGuid() => RequestContext.OwnerId is null ? null : Guid.TryParse(RequestContext.OwnerId, out Guid g) ? g : null;
}
