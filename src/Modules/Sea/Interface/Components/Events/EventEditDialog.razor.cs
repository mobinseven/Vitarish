using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventEditDialog : EventsBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public EventReadDto? Event { get; set; }

    private EventWriteDto EventWrite = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Event is null)
        {
            MudDialog.Cancel();
            return;
        }
        EventWrite = Event.ToWrite();
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
            EventReadDto? updated = await EventsService.UpdateAsync(EventWrite, CancellationToken.None).EnsureValueAsync();
            MudDialog.Close(DialogResult.Ok(updated));
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
