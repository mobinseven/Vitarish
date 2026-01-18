using Vitarish.Entities.Server.Models;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Interfaces.Base.Common;

public class CommonBase : ComponentBase, IDisposable, IRenderNotificationReceiver
{
    protected string? SearchString { get; set; }

    public Guid Id { get; private set; } = Guid.NewGuid();
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;
    [Inject] protected IStringLocalizer Localizer { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected IScrollManager ScrollManager { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;
    [CascadingParameter] public Breakpoint? PageBreakpoint { get; set; }

    private readonly IList<Type> registeredTypes = [];
    private readonly IList<string> registeredIds = [];
    protected void RenderForType<T>() => registeredTypes.Add(typeof(T));
    protected void RenderForId(string id) => registeredIds.Add(id);

    protected bool firstRenderDone;
    private IDisposable? subscription;
    protected override void OnInitialized()
    {
        subscription = RenderNotifier.Subscribe(this);

        NavigationManager.LocationChanged += OnLocationChanged;
        _ = UpdateSearchFromUri(NavigationManager.Uri);
    }
    protected virtual Task ReloadRequired() => Task.CompletedTask;

    protected virtual async void RenderNotificationReceived(object? sender, RenderNotification notification)
    {
        if (notification.Content is EntityNotificationArgs entityNotification)
        {
            bool typeAndTenant = registeredTypes.Any(t => t.Name.StartsWith(entityNotification.EntityName) && entityNotification.CorrelationId == RequestContext.TenantId);
            bool entityId = registeredIds.Any(id => entityNotification.Id == id);

            if (typeAndTenant || entityId)
            {
                await InvokeAsync(ReloadRequired);
            }
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
