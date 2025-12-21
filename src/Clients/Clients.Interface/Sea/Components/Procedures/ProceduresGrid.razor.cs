namespace Vitarish.Clients.Interface.Sea.Components.Procedures;

public partial class ProceduresGrid : ProceduresBase
{
    [Parameter, EditorRequired] public ProjectDto Project { get; set; }
    [Parameter] public EventCallback<ProjectDto> ProjectChanged { get; set; }
    [Parameter] public EventCallback ProjectDeleted { get; set; }
    [Parameter] public Guid? OpenProcedure { get; set; }
    [Parameter] public EventCallback<Guid?> OpenProcedureChanged { get; set; }
    [Parameter] public RenderFragment<ProcedureDto>? ProcedureChildContent { get; set; }

    [Inject] private IEventsClient EventsService { get; set; } = default!;

    private MudDataGrid<ProcedureDto>? grid;
    private List<ProcedureDto> procedures = [];
    private Guid? processingProcedureId;

    private async Task<GridData<ProcedureDto>> ReloadData(GridState<ProcedureDto> gridState)
    {
        ProcedureDescriptorDto descriptor = new()
        {
            RoleIds = AdminMode ? null : AccessService.RoleIds,
            ProjectId = Project.Id,
            //IncludeStats = true
        };
        IReadOnlyList<ProcedureDto> Items = await ProceduresService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load procedures");

        procedures = [.. Items
            .OrderByDescending(e => (e.Stats?.Unread ?? false) || ((e.Stats?.NewItems ?? 0) > 0))
            .ThenBy(e => e.Name)];

        return new GridData<ProcedureDto>
        {
            Items = procedures,
            TotalItems = procedures.Count
        };
    }

    protected override async Task ReloadRequired() => await grid!.ReloadServerData();


    private async Task EditProcedure(ProcedureDto procedure)
    {
        DialogParameters parameters = new()
        {
            { nameof(ProcedureEditDialog.ProjectId), Project.Id },
            { nameof(ProcedureEditDialog.Procedure), procedure },
            { nameof(ProcedureEditDialog.ProjectName), Project.Name }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ProcedureEditDialog>(Localizer["Procedure"], parameters);
        DialogResult? result = await dialog.Result;


        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }
    }

    private async Task DeleteProcedure(ProcedureDto procedure)
    {
        DialogParameters parameters = new()
        {
            { nameof(StrictConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(StrictConfirmDialog.ButtonText), Localizer["DeleteItem", Localizer["Procedure"]].Value },
            { nameof(StrictConfirmDialog.ItemName), procedure.Name },
            { nameof(StrictConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<StrictConfirmDialog>(Localizer["DeleteItem", Localizer["Procedure"]], parameters);
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
            await ProceduresService.DeleteAsync(procedure.Id, CancellationToken.None);
            _ = procedures.Remove(procedure);
            StateHasChanged();
        }
    }

    private bool IsCommitAllDisabled(ProcedureDto procedure)
    {
        if (processingProcedureId == procedure.Id)
        {
            return true;
        }

        StatsDto? stats = procedure.Stats;

        return stats is not null && (stats.Events == 0 || stats.Events == stats.CommittedEvents);
    }

    private async Task CommitAllEventsAsync(ProcedureDto procedure)
    {
        if (!Guid.TryParse(AccessService.User?.Id, out Guid userId))
        {
            return;
        }

        processingProcedureId = procedure.Id;
        StateHasChanged();

        try
        {
            DateTimeOffset commitTime = new(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));
            int committed = await EventsService.CommitAllByProcedureAsync(
                procedure.Id,
                userId,
                commitTime,
                CancellationToken.None);

            if (committed > 0)
            {
                await grid!.ReloadServerData();
            }
        }
        finally
        {
            processingProcedureId = null;
            StateHasChanged();
        }
    }

    private bool IsUncommitAllDisabled(ProcedureDto procedure)
    {
        if (processingProcedureId == procedure.Id)
        {
            return true;
        }

        StatsDto? stats = procedure.Stats;

        return stats is not null && stats.CommittedEvents == 0;
    }

    private async Task UncommitAllEventsAsync(ProcedureDto procedure)
    {
        processingProcedureId = procedure.Id;
        StateHasChanged();

        try
        {
            int uncommitted = await EventsService.UncommitAllByProcedureAsync(procedure.Id, CancellationToken.None);

            if (uncommitted > 0)
            {
                await grid!.ReloadServerData();
            }
        }
        finally
        {
            processingProcedureId = null;
            StateHasChanged();
        }
    }
}
