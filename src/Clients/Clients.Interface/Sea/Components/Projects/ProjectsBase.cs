namespace Vitarish.Clients.Interface.Sea.Components.Projects;


public class ProjectsBase : SeaCommonBase
{
    [Inject] protected IProjectsClient ProjectsService { get; set; } = default!;
    [Inject] protected IReceiptsClient ReceiptsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProjectDto>();
    }
}
