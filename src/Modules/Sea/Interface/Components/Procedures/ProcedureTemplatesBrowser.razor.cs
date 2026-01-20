namespace Vitarish.Sea.Interface.Components.Procedures;

public partial class ProcedureTemplatesBrowser : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IProjectsService ProjectsService { get; set; } = default!;
    [Inject] private IProceduresService ProceduresService { get; set; } = default!;

    private List<ProjectDto> projects = [];
    private readonly Dictionary<Guid, List<ProcedureDto>> procedures = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        projects = [.. await ProjectsService.GetListAsync(new ProjectDescriptorDto { Template = true }, CancellationToken.None)
            .EnsureValueAsync("Load template projects")];
        foreach (ProjectDto p in projects)
        {
            procedures[p.Id] = (await ProceduresService.GetListAsync(new ProcedureDescriptorDto { ProjectId = p.Id }, CancellationToken.None)
                .EnsureValueAsync("Load template procedures")).ToList();
        }
    }

    private void Select(ProcedureDto procedure) => MudDialog.Close(DialogResult.Ok(procedure));
    private void Cancel() => MudDialog.Cancel();
}
