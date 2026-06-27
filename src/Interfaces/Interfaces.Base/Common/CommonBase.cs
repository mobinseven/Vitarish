using Microsoft.Extensions.Logging;
using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Interfaces.Base.Common;

public class CommonBase : ComponentBase, IDisposable
{
    [CascadingParameter] protected string? SearchString { get; set; }

    [Inject] protected IStringLocalizer Localizer { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected IScrollManager ScrollManager { get; set; } = default!;
    [Inject] private IRenderNotifier RenderNotifier { get; set; } = default!;
    [Inject] protected IThemeService ThemeService { get; set; } = default!;
    [Inject] protected INavService NavService { get; set; } = default!;
    [Inject] private ILogger<CommonBase> Logger { get; set; } = default!;
    [CascadingParameter] public Breakpoint? PageBreakpoint { get; set; }

    private readonly IList<UiReloadSubscription> reloadSubscriptions = [];

    protected void RenderForType<T>() => RenderForSubject(typeof(T).Name);
    protected void RenderForSubject(string? subject) => reloadSubscriptions.Add(new UiReloadSubscription(subject, null));
    protected void RenderForSubjectId(string? subjectId) => reloadSubscriptions.Add(new UiReloadSubscription(null, subjectId));

    protected virtual Task OnSearchedAsync() => Task.CompletedTask;
    protected override void OnInitialized() => RenderNotifier.RenderRequired += RenderNotificationReceived;
    protected override Task OnInitializedAsync() => ReloadRequired();
    protected virtual Task ReloadRequired() => Task.CompletedTask;

    protected virtual void RenderNotificationReceived(object? sender, object args) =>
        _ = InvokeAsync(async () => await HandleRenderNotificationAsync(args).ConfigureAwait(true));

    private async Task HandleRenderNotificationAsync(object args)
    {
        try
        {
            if (args is UiReloadNotification reloadNotification && reloadSubscriptions.Count > 0)
            {
                if (MatchesReloadNotification(reloadNotification))
                {
                    await ReloadRequired().ConfigureAwait(true);
                }

                return;
            }

            if (args is SearchArgs search)
            {
                SearchString = search.SearchedValue;
                await OnSearchedAsync().ConfigureAwait(true);
                return;
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to handle UI render notification.");
        }
    }

    private bool MatchesReloadNotification(UiReloadNotification notification) =>
        reloadSubscriptions.Any(subscription =>
            MatchesOptional(subscription.Subject, notification.Subject) &&
            MatchesOptional(subscription.SubjectId, notification.SubjectId));

    private static bool MatchesOptional(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) || string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);

    void IDisposable.Dispose()
    {
        RenderNotifier.RenderRequired -= RenderNotificationReceived;
        GC.SuppressFinalize(this);
    }
}
