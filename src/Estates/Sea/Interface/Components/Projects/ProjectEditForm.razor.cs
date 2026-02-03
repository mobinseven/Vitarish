namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectEditForm : ProjectsBase
{
    [Inject] private IEnumerable<IProjectEditPlugin> ProjectEditPlugins { get; set; } = [];

    [Parameter] public ProjectDto? Project { get; set; }
    [Parameter] public EventCallback<Guid> OnUpdate { get; set; }
    [Parameter] public EventCallback<Guid> OnCreate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public bool TemplateMode { get; set; } = false;


    private IProjectEditPlugin? Plugin => ProjectEditPlugins.SingleOrDefault(p => p.Id == MetadataService?.PluginIdentifier);


    private string name = string.Empty;
    private string description = string.Empty;
    private bool template;
    private string metadata = "{}";

    protected override void OnParametersSet()
    {
        if (Project is not null)
        {
            name = Project.Name;
            description = Project.Description;
            template = Project.Template;
            metadata = Project.Metadata;
        }
    }
    protected virtual async Task SaveAsync()
    {
        if (Project is null)
        {
            var created = await ProjectsService.AddAsync(name, description, template, metadata, CancellationToken.None);
            await OnCreate.InvokeAsync(created);
        }
        else
        {
            await ProjectsService.UpdateAsync(Project.Id, name, description, template, metadata, CancellationToken.None);
            await OnUpdate.InvokeAsync(Project.Id);
        }
    }

    protected void Cancel() => OnCancel.InvokeAsync();
}
