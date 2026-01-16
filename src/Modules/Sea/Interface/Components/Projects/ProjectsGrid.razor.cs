namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectsGrid : ProjectsBase, IDisposable
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }
    [Inject] private IEnumerable<IProjectsGridPlugin> ProjectsGridPlugins { get; set; } = [];
    [Inject] private IUsageConfigurationService UsageConfigurationService { get; set; } = default!;

    private readonly bool template;

    protected string? PluginIdentifier;
    private IProjectsGridPlugin? Plugin => ProjectsGridPlugins.SingleOrDefault(p => p.Id == PluginIdentifier);

    private List<ProjectDto> projects = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UsageConfigurationDto? usageConfiguration = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
        UsageConfigurationMetadata metadata = UsageConfigurationMetadata.FromJson(usageConfiguration?.Metadata);
        PluginIdentifier = metadata.PluginIdentifier;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadData();
        }
    }

    private async Task ReloadData()
    {
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = SearchString,
            Template = AdminMode && template,
        };
        IReadOnlyList<ProjectDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load projects");

        projects = [.. Items
            .OrderByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task ReloadRequired() => await ReloadData();

    private async Task AddProject()
    {
        IDialogReference selector = await DialogService.ShowAsync<ProjectEditDialog>();
        DialogResult? result = await selector.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is ProjectDto created)
        {
            await OnCreated.InvokeAsync(created.Id);
        }
    }
}
