using Vitarish.Entities.Server.Models;
using Vitarish.Interfaces.Base.Abstractions;
using Vitarish.Interfaces.Base.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Interfaces.Base.Common;

public class CommonBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected string? SearchString { get; set; }

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
    protected virtual Task OnSearchedAsync() => Task.CompletedTask;
    protected override void OnInitialized() => RenderNotifier.RenderRequired += RenderNotificationReceived;
    protected override Task OnInitializedAsync() => ReloadRequired();
    protected virtual Task ReloadRequired() => Task.CompletedTask;

    protected virtual async void RenderNotificationReceived(object? sender, object args)
    {
        if (args is EntityNotificationArgs entityNotification)
        {
            bool typeAndTenant = registeredTypes.Any(t => t.Name.StartsWith(entityNotification.EntityName) && entityNotification.CorrelationId == RequestContext.TenantId);
            bool entityId = registeredIds.Any(id => entityNotification.Id == id);

            if (typeAndTenant || entityId)
            {
                await InvokeAsync(ReloadRequired);
            }
        }
        else if (args is SearchArgs search)
        {
            SearchString = search.SearchedValue;
            await OnSearchedAsync();
        }
        else
        {
            await InvokeAsync(StateHasChanged);
        }
    }
    void IDisposable.Dispose()
    {
        RenderNotifier.RenderRequired -= RenderNotificationReceived;
        GC.SuppressFinalize(this);
    }

}
