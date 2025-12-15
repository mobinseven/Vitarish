namespace Vitarish.Clients.Interface.Common;


public partial class FocusWrapper : ComponentBase, IAsyncDisposable
{
    [Inject] protected IFocusService FocusService { get; set; } = default!;
    [Inject] protected IJSRuntime? JS { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected string OverlayStyle = "display: none;";

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await (JS?.RegisterJsModule(this) ?? throw new InvalidOperationException());
            FocusService.FocusActivation += FocusService_FocusActivation;
        }
    }

    private async void FocusService_FocusActivation(object? sender, bool active)
    {
        if (_module != null)
        {
            if (active)
            {
                await _module.InvokeVoidAsync("stopTracking");
                ElementReference? element = sender as ElementReference?;
                await _module.InvokeVoidAsync("startTracking", element);
            }
            else
            {
                await _module.InvokeVoidAsync("stopTracking");
            }
            OverlayStyle = active ? "" : "display: none;";
            await InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.InvokeVoidAsync("stopTracking");
            await _module.DisposeAsync();
        }
        FocusService.FocusActivation -= FocusService_FocusActivation;
    }
}
