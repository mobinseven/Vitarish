namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventEditDialog : EventsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public EventReadDto? Event { get; set; }

    private string title = string.Empty;
    private string description = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
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
            EventReadDto? updated = await EventsService.UpdateAsync(Event.Id, title, description, CancellationToken.None);
            MudDialog.Close(DialogResult.Ok(updated));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
