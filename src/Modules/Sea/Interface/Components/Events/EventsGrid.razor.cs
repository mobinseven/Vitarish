namespace Vitarish.Sea.Interface.Components.Events;

public partial class EventsGrid : EventsBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; }
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; }
    [Parameter] public Guid? OpenEvent { get; set; }
    [Parameter] public EventCallback<Guid?> EventClicked { get; set; }

    [Inject] private IEnumerable<IEventCreationFormPlugin> EventCreationFormPlugins { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private List<EventDto> events = [];

    private MudDataGrid<EventDto>? grid;

    private readonly Dictionary<Guid, bool> canRead = [];
    private readonly Dictionary<Guid, bool> canWrite = [];


    private async Task<GridData<EventDto>> ReloadData(GridState<EventDto> gridState)
    {
        events = [.. (await EventsService.GetByProcedureAsync(Procedure.Id, CancellationToken.None)).SortByChain()];

        canRead.Clear();
        canWrite.Clear();

        Guid[] eventIds = [.. events.Select(e => e.Id)];

        Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> accessTask = AccessService.GetEventAccessAsync(eventIds);

        IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)> accessResults = await accessTask;
        foreach ((Guid eventId, (bool CanRead, bool CanWrite) access) in accessResults)
        {
            canRead[eventId] = access.CanRead;
            canWrite[eventId] = access.CanWrite;
        }

        return new GridData<EventDto>
        {
            Items = events,
            TotalItems = events.Count
        };
    }

    protected override Task ReloadRequired() => grid?.ReloadServerData() ?? Task.CompletedTask;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (Procedure is not null)
        {
            await EventsService.RepairChainAsync(Procedure.Id, CancellationToken.None);
        }
    }

    private bool IsFirst(EventDto e) => events.GetFirstByChain()?.Id == e.Id;
    private bool IsLast(EventDto e) => events.GetLastByChain()?.Id == e.Id;

    private Task AddEvent() => AddEventAt(events.GetLastByChain()?.Id);

    private Task AddEventAfter(EventDto item) => AddEventAt(item.Id);

    private EventCreationFormContext BuildCreationFormContext(Guid? previousEventId)
        => new(ServiceProvider, Project, Procedure, previousEventId, new Dictionary<string, object?>
        {
            { "ProjectId", Project.Id },
            { "ProcedureId", Procedure.Id },
            { "PreviousEventId", previousEventId }
        });

    private async Task ShowPluginAsync(IEventCreationFormPlugin plugin, EventCreationFormContext context)
    {
        DialogParameters parameters = new()
        {
            { nameof(PluginDialog<EventCreationFormContext>.Plugin), plugin },
            { nameof(PluginDialog<EventCreationFormContext>.Context), context }
        };

        _ = await DialogService.ShowAsync<PluginDialog<EventCreationFormContext>>(plugin.DisplayName, parameters);
    }

    private async Task AddEventAt(Guid? previousEventId)
    {
        EventCreationFormContext context = BuildCreationFormContext(previousEventId);
        List<IEventCreationFormPlugin> plugins =
            [.. EventCreationFormPlugins
                .Where(plugin => plugin.IsAvailable(context))
                .OrderBy(plugin => plugin.DisplayName, StringComparer.CurrentCultureIgnoreCase)];

        if (plugins.Count == 0)
        {
            await ShowEditDialogForNewEventAsync(previousEventId);
            return;
        }

        if (plugins.Count == 1)
        {
            await ShowPluginAsync(plugins[0], context);
            return;
        }

        DialogParameters selectorParameters = new()
        {
            { nameof(EventCreationFormSelectorDialog.Options), plugins },
            { nameof(EventCreationFormSelectorDialog.ShowStandardForm), true }
        };

        IDialogReference selector = await DialogService.ShowAsync<EventCreationFormSelectorDialog>(Localizer["Event"], selectorParameters);
        DialogResult? result = await selector.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is IEventCreationFormPlugin plugin)
        {
            await ShowPluginAsync(plugin, context);
            return;
        }

        await ShowEditDialogForNewEventAsync(previousEventId);
    }

    private async Task ShowEditDialogForNewEventAsync(Guid? previousEventId)
    {
        EventDto? created = await EventsService.AddAsync(Project.Id, Procedure.Id, previousEventId, CancellationToken.None);

        if (grid is not null)
        {
            await grid.ReloadServerData();
        }

        if (created is not null)
        {
            await EditEvent(created);
        }
    }

    private async Task EditEvent(EventDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EventEditDialog.Event), item }
        };

        _ = await DialogService.ShowAsync<EventEditDialog>(Localizer["Event"], parameters);
    }


    private async Task MoveUp(EventDto item)
    {
        if (await EventsService.MoveUpAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            if (grid is not null)
            {
                await grid.ReloadServerData();
            }
        }
    }

    private async Task MoveDown(EventDto item)
    {
        if (await EventsService.MoveDownAsync(Procedure.Id, item.Id, CancellationToken.None))
        {
            if (grid is not null)
            {
                await grid.ReloadServerData();
            }
        }
    }


    private async Task ToggleCommitAsync(EventDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EventCommitDetails.Event), item }
        };

        _ = await DialogService.ShowAsync<EventCommitDetails>(Localizer["Event"], parameters);
    }

}
