namespace Vitarish.Interfaces.Base.Common;

public partial class EndOfScroll : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private IJSObjectReference? module;
    private ElementReference placeholder;
    private double appliedHeight = double.NaN;
    private double appliedDuration = double.NaN;
    private bool initialized;

    [Parameter]
    public string? CssClass { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public double Height { get; set; } = 72d;

    [Parameter]
    public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromMilliseconds(220);

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClassValue => string.IsNullOrWhiteSpace(CssClass)
        ? "vorn-end-of-scroll"
        : $"vorn-end-of-scroll {CssClass}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.RegisterJsModule(this);
        }

        if (module is null)
        {
            return;
        }

        double duration = TransitionDuration.TotalMilliseconds;
        if (!initialized)
        {
            await module.InvokeVoidAsync("initialize", placeholder, Height, duration);
            appliedHeight = Height;
            appliedDuration = duration;
            initialized = true;
            return;
        }

        if (!Height.Equals(appliedHeight) || !duration.Equals(appliedDuration))
        {
            await module.InvokeVoidAsync("update", placeholder, Height, duration);
            appliedHeight = Height;
            appliedDuration = duration;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("dispose", placeholder);
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Blazor disposes JSRuntime during shutdown – ignore.
        }
        finally
        {
            module = null;
        }
    }
}
