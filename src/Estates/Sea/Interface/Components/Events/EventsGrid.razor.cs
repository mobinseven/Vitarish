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
        events = [.. (await EventsService.GetByProcedureAsync(Procedure.Id, CancellationToken.None)).SortByChain()];

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
        EventReadDto? created = await EventsService.AddAsync(Project.Id, Procedure.Id, previousEventId, CancellationToken.None);
        if (created is not null)
        {
            EventReadDto? updated = await EventsService.UpdateAsync(created.Id, $"{Localizer["Event"]} {events.Count}", string.Empty, CancellationToken.None);
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

        var dialog = await DialogService.ShowAsync<EventEditDialog>(null, parameters);
        var result = dialog.Result;
        if (result is null || result.IsCanceled)
        {
            return;
        }
        await ReloadRequired();
        await OnUpdate.InvokeAsync();
    }


    private async Task MoveUp(EventReadDto item)
    {
        if (await EventsService.MoveUpAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            await ReloadRequired();
        }
    }

    private async Task MoveDown(EventReadDto item)
    {
        if (await EventsService.MoveDownAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            await ReloadRequired();
        }
    }
}
