
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Immutable;

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
    private Guid? draggedEventId;
    private Guid? dropIndicatorEventId;
    private bool dropIndicatorBefore;

    //Action<bool> onExpandedChanged(EventDto item) => async e =>
    //{
    //    if (!e)
    //    {
    //        await OpenEventChanged.InvokeAsync(e ? item.Id : null);
    //        return;
    //    }
    //    await grid!.ReloadServerData();
    //};

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

    protected override async Task ReloadRequired() => await grid?.ReloadServerData();

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

    private bool CanReorder(EventDto item)
        => AdminMode || (canWrite.TryGetValue(item.Id, out bool value) && value);

    private bool CanReorderEvents
        => AdminMode || events.Any(item => canWrite.TryGetValue(item.Id, out bool value) && value);

    private bool ShouldShowDropZones => CanReorderEvents && draggedEventId is Guid;

    private string GetRowClass(EventDto item)
    {
        List<string> classes = ["event-row"];

        if (!CanReorder(item))
        {
            classes.Add("event-row--locked");
        }

        if (draggedEventId == item.Id)
        {
            classes.Add("event-row--dragging");
        }

        return string.Join(' ', classes);
    }

    private string GetDropZoneCss(Guid? targetId, bool before)
        => dropIndicatorEventId == targetId && dropIndicatorBefore == before
            ? "event-drop-zone event-drop-zone--active"
            : "event-drop-zone";

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

    private async Task EditEventRoles(EventDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(EventEditDialog.Event), item }
        };

        _ = await DialogService.ShowAsync<EventRolesEditDialog>(Localizer["Event"], parameters);
    }

    private async Task DeleteEvent(EventDto item)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["Delete"], parameters);
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
            _ = await EventsService.RemoveEventAsync(Procedure.Id, item.Id, CancellationToken.None);
            if (grid is not null)
            {
                await grid.ReloadServerData();
            }
        }
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

    private void HandleDragStart(EventDto item, DragEventArgs _)
    {
        if (!CanReorder(item))
        {
            return;
        }

        draggedEventId = item.Id;
        dropIndicatorEventId = null;
        dropIndicatorBefore = false;
        StateHasChanged();
    }

    private void HandleDragEnd(DragEventArgs _)
        => ResetDragState();

    private void HandleDragOver(Guid? targetEventId, bool before, DragEventArgs _)
    {
        if (draggedEventId is null)
        {
            return;
        }

        if (targetEventId == draggedEventId && before)
        {
            if (dropIndicatorEventId is not null)
            {
                dropIndicatorEventId = null;
                StateHasChanged();
            }
            return;
        }

        if (dropIndicatorEventId != targetEventId || dropIndicatorBefore != before)
        {
            dropIndicatorEventId = targetEventId;
            dropIndicatorBefore = before;
            StateHasChanged();
        }
    }

    private void HandleDragLeave(Guid? targetEventId, bool before, DragEventArgs _)
    {
        if (dropIndicatorEventId == targetEventId && dropIndicatorBefore == before)
        {
            dropIndicatorEventId = null;
            StateHasChanged();
        }
    }

    private async Task HandleDropAsync(Guid? targetEventId, bool before, DragEventArgs d)
    {
        if (draggedEventId is not Guid sourceId)
        {
            ResetDragState();
            return;
        }

        List<EventDto> reordered = [.. events];
        EventDto? dragged = reordered.FirstOrDefault(e => e.Id == sourceId);

        if (dragged is null)
        {
            ResetDragState();
            return;
        }

        _ = reordered.Remove(dragged);

        int insertIndex = reordered.Count;

        if (targetEventId is Guid targetId)
        {
            int targetIndex = reordered.FindIndex(e => e.Id == targetId);

            if (targetIndex < 0)
            {
                ResetDragState();
                return;
            }

            insertIndex = before ? targetIndex : targetIndex + 1;
        }

        insertIndex = Math.Clamp(insertIndex, 0, reordered.Count);
        reordered.Insert(insertIndex, dragged);

        if (reordered.Select(e => e.Id).SequenceEqual(events.Select(e => e.Id)))
        {
            ResetDragState();
            return;
        }

        try
        {
            await PersistChainAsync(reordered);

            if (grid is not null)
            {
                await grid.ReloadServerData();
            }
        }
        finally
        {
            ResetDragState();
        }
    }

    private async Task PersistChainAsync(IReadOnlyList<EventDto> orderedEvents)
    {
        List<EventChainChangeDto> chain = [];
        Guid? previousId = null;

        foreach (EventDto evt in orderedEvents)
        {
            chain.Add(new EventChainChangeDto(evt.Id, previousId));
            previousId = evt.Id;
        }

        await EventsService.UpdateChainAsync(Procedure.Id, chain, CancellationToken.None);
    }

    private void ResetDragState()
    {
        draggedEventId = null;
        dropIndicatorEventId = null;
        dropIndicatorBefore = false;
        StateHasChanged();
    }
}
