namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectsGrid : ProjectsBase, IDisposable
{
    [Parameter] public bool TemplateMode { get; set; } = false;
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback OnAdd { get; set; }
    [Inject] private IEnumerable<IProjectsGridPlugin> ProjectsGridPlugins { get; set; } = [];
    private IProjectsGridPlugin? Plugin => ProjectsGridPlugins.SingleOrDefault(p => p.Id == UsageConfigurationMetadata?.PluginIdentifier);

    protected List<ProjectDto> projects = [];

    protected override async Task ReloadRequired()
    {
        if (Plugin is not null)
        {
            return;
        }

        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = SearchString,
            Template = TemplateMode
        };

        IReadOnlyList<ProjectDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync() ?? [];

        projects = [.. Items
            .OrderByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];

        await InvokeAsync(StateHasChanged);
    }
    protected virtual Task AddProject() => OnAdd.InvokeAsync();
}
