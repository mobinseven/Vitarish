using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;

namespace Vitarish.Parties.Interface.Components;

/// <summary>
/// Base class for all owner-facing interface components.
/// Injects the parties service and snackbar so every component can query/mutate owner data and surface feedback.
/// </summary>
public class OwnerCommonBase : CommonBase
{
    [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
}
