namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureTemplatesBrowser : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IProjectsService ProjectsService { get; set; } = default!;
    [Inject] private IProceduresService ProceduresService { get; set; } = default!;

    private List<ProjectReadDto> projects = [];
    private readonly Dictionary<Guid, List<ProcedureReadDto>> procedures = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        projects = [.. await ProjectsService.GetListAsync(new ProjectDescriptorDto { Template = true }, CancellationToken.None).GetValueOrDefaultAsync() ?? []];
        foreach (ProjectReadDto p in projects)
        {
            procedures[p.Id] = [.. await ProceduresService.GetListAsync(new ProcedureDescriptorDto { ProjectId = p.Id }, CancellationToken.None).GetValueOrDefaultAsync() ?? []];
        }
    }

    private void Select(ProcedureReadDto procedure) => MudDialog.Close(DialogResult.Ok(procedure));
    private void Cancel() => MudDialog.Cancel();
}
