namespace Vitarish.Interfaces.Base.Common;

public partial class PwaInstall : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, EditorRequired] public required RenderFragment<PwaInstallContext> InstallButton { get; set; }
    [Parameter] public RenderFragment? Placeholder { get; set; }
    [Parameter] public EventCallback InstallAvailableCallback { get; set; }
    [Parameter] public EventCallback InstallAcceptedCallback { get; set; }
    [Parameter] public EventCallback InstallDismissedCallback { get; set; }
    public class PwaInstallContext(Action action)
    {
        public Action OnInstallClicked { get; init; } = action;
    }
    private PwaInstallContext InstallContext => new(async () => await OnInstallClicked());
    private IJSObjectReference? _module;
    private readonly ElementReference _installButton = default;
    private DotNetObjectReference<PwaInstall>? _objRef;

    private bool installAvaiable;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.RegisterJsModule(this);
            _objRef = DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync(
                "initializePwaInstall", _installButton, _objRef);
        }
    }

    private async Task OnInstallClicked()
        => await _module!.InvokeVoidAsync("triggerInstall");

    [JSInvokable]
    public async Task OnInstallAvailable()
    {
        installAvaiable = true;
        await InstallAvailableCallback.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Called from JS with the user's decision <c>"accepted"</c> or <c>"dismissed"</c>.</summary>
    [JSInvokable]
    public async Task OnAppInstalled(string outcome)
    {
        if (outcome == "accepted")
        {
            await InstallAcceptedCallback.InvokeAsync();
        }
        else
        {
            await InstallDismissedCallback.InvokeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("dispose");
            await _module.DisposeAsync();
        }
        _objRef?.Dispose();
    }
}
