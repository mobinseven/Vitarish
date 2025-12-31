namespace Vitarish.Sea.Interface.Components.Projects;


public class ProjectsBase : SeaCommonBase
{
    [Inject] protected IProjectsService ProjectsService { get; set; } = default!;
    [Inject] protected IReceiptsService ReceiptsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProjectDto>();
    }
}
