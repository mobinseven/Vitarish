namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProceduresGrid : ProceduresBase
{
    [Parameter, EditorRequired] public ProjectReadDto Project { get; set; }
    [Parameter] public EventCallback<Guid?> OnClicked { get; set; }
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }

    private List<ProcedureReadDto> procedures = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadData();
            StateHasChanged();
        }
    }
        
    private async Task ReloadData()
    {
        ProcedureDescriptorDto descriptor = new()
        {
            ProjectId = Project.Id
        };
        IReadOnlyList<ProcedureReadDto> Items = await ProceduresService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        procedures = [.. Items
            .OrderByDescending(e => (e.Stats?.Unread ?? false) || ((e.Stats?.NewItems ?? 0) > 0))
            .ThenBy(e => e.Name)];
    }

    protected override async Task ReloadRequired() => await ReloadData();
    private async Task AddProcedure()
    {
        DialogParameters parameters = new()
        {
            { nameof(ProcedureCreateDialog.ProjectId), Project.Id },
            { nameof(ProcedureCreateDialog.ProjectName), Project.Name }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ProcedureCreateDialog>(null, parameters);

        DialogResult? result = await dialog.Result;

        if (result?.Canceled ?? true)
        {
            return;
        }

        if (result.Data is Guid procedureId)
        {
            await OnCreated.InvokeAsync(procedureId);
        }
    }

}
