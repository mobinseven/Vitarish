using Vitarish.Clients.Interface.Sea.Abstractions;

namespace Vitarish.Clients.Interface.Sea.Components.Base;

public class SeaCommonBase : CommonBase
{
    [Inject] protected ISeaAccessService AccessService { get; set; } = default!;
    /// <summary>
    /// Optional override for admin-specific rendering when the caller wants to force admin mode.
    /// </summary>
    [Parameter] public bool? IsAdmin { get; set; }
    /// <summary>
    /// Effective admin flag resolved from the override or the access service.
    /// </summary>
    protected bool AdminMode => IsAdmin ?? AccessService.IsAdmin;
    public override void OnCompleted() => throw new NotImplementedException();
    public override void OnError(Exception error) => throw new NotImplementedException();
    protected override Task ReloadRequired() => Task.CompletedTask;
}
