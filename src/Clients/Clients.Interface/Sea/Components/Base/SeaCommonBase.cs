using Vitarish.Clients.Interface.Sea.Abstractions;

namespace Vitarish.Clients.Interface.Sea.Components.Base;

public class SeaCommonBase : CommonBase
{
    [Inject] protected ISeaAccessService AccessService { get; set; } = default!;
    public override void OnCompleted() => throw new NotImplementedException();
    public override void OnError(Exception error) => throw new NotImplementedException();
    protected override Task ReloadRequired() => Task.CompletedTask;
}

