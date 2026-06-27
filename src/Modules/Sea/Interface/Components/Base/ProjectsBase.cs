namespace Vitarish.Sea.Interface.Components.Base;

public class ProjectsBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProjectReadDto>();
    }
}
