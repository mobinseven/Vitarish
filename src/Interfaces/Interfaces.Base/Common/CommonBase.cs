using Vitarish.Interfaces.Base.Abstractions;

namespace Vitarish.Interfaces.Base.Common;

public class CommonBase : ComponentBase, IDisposable, IRenderNotificationReceiver
{
    protected string? SearchString { get; set; }

    public Guid Id { get; private set; } = Guid.NewGuid();
    [Inject] protected IStringLocalizer Localizer { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected IScrollManager ScrollManager { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;
    [CascadingParameter] public Breakpoint? PageBreakpoint { get; set; }

    protected bool firstRenderDone;
    private readonly IList<Type> registeredTypes = [];
    private IDisposable? subscription;
    protected void RenderForType<T>() => registeredTypes.Add(typeof(T));
    protected override void OnInitialized()
    {
        subscription = RenderNotifier.Subscribe(this);

        NavigationManager.LocationChanged += OnLocationChanged;
        _ = UpdateSearchFromUri(NavigationManager.Uri);
    }
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
    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            bool updated = UpdateSearchFromUri(e.Location);
            if (updated)
            {
                await ReloadRequired();
            }
        }
        catch
        {
            // Ignore navigation refresh errors.
        }
    }

    private bool UpdateSearchFromUri(string uri)
    {
        string? query = SearchQueryHelper.ExtractQuery(NavigationManager, uri);

        if (string.Equals(SearchString, query, StringComparison.Ordinal))
        {
            return false;
        }

        SearchString = query;
        return true;
    }
    void IDisposable.Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        subscription?.Dispose();
        GC.SuppressFinalize(this);
    }
    public virtual void OnCompleted() { }
    public virtual void OnError(Exception error) { }
    void IObserver<RenderNotification>.OnNext(RenderNotification value) => RenderNotificationReceived(this, value);

}
