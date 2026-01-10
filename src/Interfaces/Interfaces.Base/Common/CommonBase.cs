using Vitarish.Interfaces.Base.Abstractions;

namespace Vitarish.Interfaces.Base.Common;

public class CommonBase : ComponentBase, IDisposable, IRenderNotificationReceiver
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    [Inject] protected IStringLocalizer Localizer { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected IScrollManager ScrollManager { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [CascadingParameter] public Breakpoint? PageBreakpoint { get; set; }

    protected bool firstRenderDone;
    private readonly IList<Type> registeredTypes = [];
    private IDisposable? subscription;
    protected void RenderForType<T>() => registeredTypes.Add(typeof(T));
    protected override void OnInitialized() => subscription = RenderNotifier.Subscribe(this);
    protected virtual Task ReloadRequired() => Task.CompletedTask;
    private async void RenderNotificationReceived(object? sender, RenderNotification notification)
    {
        bool shouldRender = registeredTypes.Any(t => t.Name.StartsWith(notification.Content));
        if (shouldRender)
        {
            await InvokeAsync(ReloadRequired);
        }
    }
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            firstRenderDone = true;
            StateHasChanged();
        }
    }
    void IDisposable.Dispose()
    {
        subscription?.Dispose();
        GC.SuppressFinalize(this);
    }
    public virtual void OnCompleted() { }
    public virtual void OnError(Exception error) { }
    void IObserver<RenderNotification>.OnNext(RenderNotification value) => RenderNotificationReceived(this, value);

}
