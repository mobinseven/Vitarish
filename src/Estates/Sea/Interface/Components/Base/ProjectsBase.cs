namespace Vitarish.Sea.Interface.Components.Base;


public class ProjectsBase : SeaCommonBase
{
    [Inject] protected IAccessControlledProjectsService ProjectsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProjectDto>();
    }
}
