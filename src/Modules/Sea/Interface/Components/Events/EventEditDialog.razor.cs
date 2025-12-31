namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventEditDialog : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IEventsService EventsService { get; set; } = default!;

    [Parameter] public EventDto? Event { get; set; }

    private string title = string.Empty;
    private string description = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        title = Event.Title;
        description = Event.Description;
    }

    private async Task SaveAsync()
    {
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        else
        {
            EventDto? updated = await EventsService.UpdateAsync(Event.Id, title, description, CancellationToken.None);
            MudDialog.Close(DialogResult.Ok(updated));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
