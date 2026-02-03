namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventsGrid : EventsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; }
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; }
    [Parameter] public EventCallback<Guid?> EventClicked { get; set; }

    private List<EventDto> events = [];

    private async Task ReloadData()
    {
        events = [.. (await EventsService.GetByProcedureAsync(Procedure.Id, CancellationToken.None)).SortByChain()];

        Guid[] eventIds = [.. events.Select(e => e.Id)];

        await InvokeAsync(StateHasChanged);

    }

    protected override Task ReloadRequired() => ReloadData();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Procedure is not null)
        {
            await ReloadData();
        }
    }

    private bool IsFirst(EventDto e) => events.GetFirstByChain()?.Id == e.Id;
    private bool IsLast(EventDto e) => events.GetLastByChain()?.Id == e.Id;

    private Task AddEvent() => AddEventAt(events.GetLastByChain()?.Id);

    private Task AddEventAfter(EventDto item) => AddEventAt(item.Id);

    private async Task AddEventAt(Guid? previousEventId)
    {
        EventDto? created = await EventsService.AddAsync(Project.Id, Procedure.Id, previousEventId, CancellationToken.None);
        if (created is not null)
        {
            EventDto? updated = await EventsService.UpdateAsync(created.Id, $"{Localizer["Event"]} {events.Count}", string.Empty, CancellationToken.None);
            if (updated is not null)
            {
                await EditEvent(updated);
            }
        }
    }

    private async Task EditEvent(EventDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EventEditDialog.Event), item }
        };

        _ = await DialogService.ShowAsync<EventEditDialog>(null, parameters);
    }


    private async Task MoveUp(EventDto item)
    {
        if (await EventsService.MoveUpAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            await ReloadData();
        }
    }

    private async Task MoveDown(EventDto item)
    {
        if (await EventsService.MoveDownAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            await ReloadData();
        }
    }
}
