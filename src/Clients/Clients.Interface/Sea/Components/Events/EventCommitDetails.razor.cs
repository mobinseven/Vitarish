namespace Vitarish.Clients.Interface.Sea.Components.Events;

public partial class EventCommitDetails : SeaCommonBase
{
    [Inject] protected IEventsClient EventsService { get; set; } = default!;
    [Inject] protected IRemindersClient RemindersService { get; set; } = default!;
    [Inject] protected IEventRolesClient EventRolesService { get; set; } = default!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    private bool CanWrite { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _ = LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        CanWrite = AdminMode || await AccessService.CanWriteEvent(Event.Id);
        await InvokeAsync(StateHasChanged);
    }
    private string? UserIdToName(Guid id) => AccessService.Users.FirstOrDefault(x => x.Id == id.ToString())?.FullName;

    private async void ToggleCommitAsync()
    {
        if (Event.IsCommitted)
        {
            DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["UncommitHelpText"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Uncommit", Event.Title].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
            IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["Uncommit"], parameters);
            DialogResult? result = await dialog.Result;

            if (result is null)
            {
                return;
            }

            if (result.Canceled)
            {
                return;
            }
            else
            {
            }
            await EventsService.UncommitAsync(Event.Id, CancellationToken.None);
        }
        else
        {
            DateTimeOffset commitTime = new(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));
            await EventsService.CommitAsync(Event.Id, Guid.Parse(AccessService.User!.Id), commitTime, CancellationToken.None);
        }
        MudDialog.Close();
    }
}
