namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventsGrid : EventsBase
{
    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; }
    [Parameter, EditorRequired] public ProcedureReadDto Procedure { get; set; }
    [Parameter] public EventCallback<Guid?> EventClicked { get; set; }
    [Parameter] public EventCallback OnUpdate { get; set; }
    private List<EventReadDto> events = [];

    protected override async Task ReloadRequired()
    {
        events = [.. (await ValueOrNotifyAsync(SeaClient.ListEventsByProcedureAsync(Procedure.Id, CancellationToken.None)) ?? []).SortByChain()];

        Guid[] eventIds = [.. events.Select(e => e.Id)];

        await InvokeAsync(StateHasChanged);

    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Procedure is not null)
        {
            await ReloadRequired();
        }
    }

    private bool IsFirst(EventReadDto e) => events.GetFirstByChain()?.Id == e.Id;
    private bool IsLast(EventReadDto e) => events.GetLastByChain()?.Id == e.Id;

    private Task AddEvent() => AddEventAt(events.GetLastByChain()?.Id);

    private Task AddEventAfter(EventReadDto item) => AddEventAt(item.Id);

    private async Task AddEventAt(Guid? previousEventId)
    {
        SeaCommandResult? createResult = await ValueOrNotifyAsync(SeaClient.CreateEventAfterAsync(previousEventId, new EventWriteDto { ProjectId = Project.Id, ProcedureId = Procedure.Id, PreviousEventId = previousEventId }, CancellationToken.None));
        EventReadDto? created = createResult?.EntityId is Guid createdId ? await ValueOrNotifyAsync(SeaClient.GetEventAsync(createdId, CancellationToken.None)) : null;
        if (created is not null)
        {
    private async Task AddEventAt(Guid? previousEventId)
    {
        SeaCommandResult? createResult = await ValueOrNotifyAsync(SeaClient.CreateEventAfterAsync(previousEventId, new EventWriteDto { ProjectId = Project.Id, ProcedureId = Procedure.Id, PreviousEventId = previousEventId }, CancellationToken.None));
        EventReadDto? created = createResult?.EntityId is Guid createdId ? await ValueOrNotifyAsync(SeaClient.GetEventAsync(createdId, CancellationToken.None)) : null;
        if (created is not null)
        {
            bool updateSuccess = await CommandOrNotifyAsync(SeaClient.UpdateEventAsync(new EventWriteDto { Id = created.Id, ProjectId = Project.Id, ProcedureId = Procedure.Id, Title = $"{Localizer["Event"]} {events.Count}", Description = string.Empty }, CancellationToken.None));
            EventReadDto? updated = updateSuccess ? await ValueOrNotifyAsync(SeaClient.GetEventAsync(created.Id, CancellationToken.None)) : null;
            if (updated is not null)
            {
                await EditEvent(updated);
            }
        }
    }

    private async Task EditEvent(EventReadDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EventEditDialog.Event), item }
        };

        IDialogReference dialog = await DialogService.ShowAsync<EventEditDialog>(null, parameters);
        Task<DialogResult?>? result = dialog.Result;
        if (result is null || result.IsCanceled)
        {
            return;
        }
        await ReloadRequired();
        await OnUpdate.InvokeAsync();
    }


    private async Task MoveUp(EventReadDto item)
    {
        if (await CommandOrNotifyAsync(SeaClient.MoveEventUpAsync(item.Id, CancellationToken.None)))
        {
            await ReloadRequired();
        }
    }

    private async Task MoveDown(EventReadDto item)
    {
        if (await CommandOrNotifyAsync(SeaClient.MoveEventDownAsync(item.Id, CancellationToken.None)))
        {
            await ReloadRequired();
        }
    }
}
