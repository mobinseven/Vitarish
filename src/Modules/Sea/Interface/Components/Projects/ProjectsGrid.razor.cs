using Microsoft.AspNetCore.Components.Routing;

namespace Vitarish.Sea.Interface.Components.Projects;

public partial class ProjectsGrid : ProjectsBase, IDisposable
{
    private string? SearchString { get; set; }

    [Parameter] public EventCallback<Guid> OnClicked { get; set; }
    [Parameter] public EventCallback<ProjectDto> OnProjectCreated { get; set; }
    [Parameter] public RenderFragment<ProjectDto>? ProjectChildContent { get; set; }
    [Inject] private IEnumerable<IProjectCreationFormPlugin> ProjectCreationFormPlugins { get; set; } = default!;
    [Inject] private IEnumerable<IProjectGridCellDisplayPlugin> ProjectGridCellDisplayPlugins { get; set; } = default!;
    [Inject] private IUsageConfigurationService UsageConfigurationService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] private IStringLocalizerFactory StringLocalizerFactory { get; set; } = default!;

    private bool template;
    private MudDataGrid<ProjectDto>? grid;
    private List<ProjectDto>? projects = null;
    private IReadOnlyList<IProjectGridCellDisplayPlugin> projectGridCellDisplayPlugins = [];
    private string? preferredProjectGridCellDisplayPluginId;
    private readonly ConcurrentDictionary<string, ProjectCreationFormPluginDisplayMetadata> pluginDisplayMetadataCache = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        projectGridCellDisplayPlugins = ProjectGridCellDisplayPlugins.ToList();
        UsageConfigurationDto? usageConfiguration = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
        UsageConfigurationMetadata metadata = UsageConfigurationMetadata.FromJson(usageConfiguration?.Metadata);
        preferredProjectGridCellDisplayPluginId = metadata.ProjectGridCellDisplayPluginId;
    }

    private async Task<GridData<ProjectDto>> ReloadData(GridState<ProjectDto> gridState)
    {
        var roleIds = await AccessService.GetRoleIds();
        ProjectDescriptorDto descriptor = new()
        {
            Skip = 0,
            Take = int.MaxValue,
            Search = SearchString,
            Template = AdminMode && template,
            RoleIds = AdminMode ? null : roleIds,
            //IncludeStats = true
        };
        IReadOnlyList<ProjectDto> Items = await ProjectsService.GetListAsync(descriptor, CancellationToken.None)
            .EnsureValueAsync("Load projects");

        projects = [.. Items
            .OrderByDescending(e => (e.Stats?.NewItems ?? 0) > 0)
            .ThenByDescending(e => e.Stats?.Unread ?? false)
            .ThenBy(e => e.Name)];

        return new GridData<ProjectDto>
        {
            Items = projects,
            TotalItems = Items.Count
        };
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        NavigationManager.LocationChanged += OnLocationChanged;
        UpdateSearchFromUri(NavigationManager.Uri);
        StateHasChanged();
    }
    protected override async Task ReloadRequired() => await grid!.ReloadServerData();

    private async void ToggleTemplate()
    {
        template = !template;
        await ReloadRequired();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        bool updated = UpdateSearchFromUri(e.Location);
        if (updated && grid is not null)
        {
            await grid.ReloadServerData();
        }
    }

    private bool UpdateSearchFromUri(string uri)
    {
        string? query = ExtractQuery(uri);

        if (string.Equals(SearchString, query, StringComparison.Ordinal))
        {
            return false;
        }

        SearchString = query;
        return true;
    }

    private string? ExtractQuery(string uri)
    {
        Uri absolute = NavigationManager.ToAbsoluteUri(uri);
        string query = absolute.Query;
        if (string.IsNullOrEmpty(query) || query.Length <= 1)
        {
            return null;
        }

        foreach (string segment in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            if (!string.Equals(segment[..separatorIndex], "q", StringComparison.Ordinal))
            {
                continue;
            }

            string value = segment[(separatorIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }

        return null;
    }

    private async Task AddProject()
    {
        ProjectCreationFormContext context = BuildCreationFormContext();
        List<(IProjectCreationFormPlugin Plugin, ProjectCreationFormPluginDisplayMetadata Metadata)> pluginOptions =
            [.. ProjectCreationFormPlugins
                .Where(plugin => plugin.IsAvailable(context))
                .Select(plugin => (Plugin: plugin, Metadata: ResolvePluginDisplayMetadata(plugin)))
                .OrderBy(option => option.Metadata.DisplayName, StringComparer.CurrentCultureIgnoreCase)];

        if (pluginOptions.Count == 0)
        {
            await ShowStandardProjectDialogAsync();
            return;
        }

        if (pluginOptions.Count == 1)
        {
            await ShowPluginAsync(pluginOptions[0].Plugin, context);
            return;
        }

        List<IProjectCreationFormPlugin> plugins = [.. pluginOptions.Select(option => option.Plugin)];
        Dictionary<string, ProjectCreationFormPluginDisplayMetadata> metadataLookup =
            pluginOptions.ToDictionary(
                option => option.Plugin.Id,
                option => option.Metadata,
                StringComparer.OrdinalIgnoreCase);

        DialogParameters parameters = new()
        {
            { nameof(ProjectCreationFormSelectorDialog.Options), plugins },
            { nameof(ProjectCreationFormSelectorDialog.DisplayMetadata), metadataLookup },
            { nameof(ProjectCreationFormSelectorDialog.ShowStandardForm), true }
        };

        IDialogReference selector = await DialogService.ShowAsync<ProjectCreationFormSelectorDialog>(Localizer["Project"], parameters);
        DialogResult? result = await selector.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        if (result.Data is IProjectCreationFormPlugin plugin)
        {
            await ShowPluginAsync(plugin, context);
            return;
        }

        await ShowStandardProjectDialogAsync();
    }

    private ProjectCreationFormContext BuildCreationFormContext() => new(ServiceProvider, null, new Dictionary<string, object?>
        {
            { ProjectPluginsConstants.TemplateCreationMetadataKey, template }
        });

    private async Task ShowStandardProjectDialogAsync()
    {
        if (template)
        {
            _ = await DialogService.ShowAsync<ProjectTemplateEditDialog>(Localizer["TemplateItem", Localizer["Project"]], []);
            return;
        }

        _ = await DialogService.ShowAsync<ProjectEditDialog>(Localizer["Project"], []);
    }

    private async Task ShowPluginAsync(IProjectCreationFormPlugin plugin, ProjectCreationFormContext context)
    {
        DialogParameters parameters = new()
        {
            { nameof(PluginDialog<ProjectCreationFormContext>.Plugin), plugin },
            { nameof(PluginDialog<ProjectCreationFormContext>.Context), context }
        };

        _ = await DialogService.ShowAsync<PluginDialog<ProjectCreationFormContext>>(null, parameters);
    }

    private ProjectCreationFormPluginDisplayMetadata ResolvePluginDisplayMetadata(IProjectCreationFormPlugin plugin) => pluginDisplayMetadataCache.GetOrAdd(plugin.Id, _ =>
                                                                                                                                 {
                                                                                                                                     string displayName = plugin.DisplayName;
                                                                                                                                     string? description = plugin.Description;

                                                                                                                                     IStringLocalizer? localizer = CreatePluginLocalizer(plugin);
                                                                                                                                     if (localizer is not null)
                                                                                                                                     {
                                                                                                                                         displayName = GetLocalizedValue(localizer, "DisplayName") ?? displayName;
                                                                                                                                         description = GetLocalizedValue(localizer, "Description") ?? description;
                                                                                                                                     }

                                                                                                                                     return new ProjectCreationFormPluginDisplayMetadata(displayName, description);
                                                                                                                                 });

    private IStringLocalizer? CreatePluginLocalizer(IProjectCreationFormPlugin plugin)
    {
        try
        {
            return StringLocalizerFactory.Create(plugin.GetType());
        }
        catch
        {
            return null;
        }
    }

    private static string? GetLocalizedValue(IStringLocalizer localizer, string name)
    {
        LocalizedString value = localizer[name];
        return value.ResourceNotFound || string.IsNullOrWhiteSpace(value.Value)
            ? null
            : value.Value;
    }

    private RenderFragment RenderProjectCell(ProjectDto project)
    {
        ProjectGridCellDisplayContext context = BuildDisplayContext(project);
        IProjectGridCellDisplayPlugin? plugin = ResolveProjectCellDisplayPlugin(context);

        return plugin?.Render(context) ?? DefaultProjectCell(context);
    }

    private IProjectGridCellDisplayPlugin? ResolveProjectCellDisplayPlugin(ProjectGridCellDisplayContext context)
    {
        List<IProjectGridCellDisplayPlugin> available =
            [.. projectGridCellDisplayPlugins.Where(plugin => plugin.IsAvailable(context))];

        if (available.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(preferredProjectGridCellDisplayPluginId))
        {
            IProjectGridCellDisplayPlugin? configured =
                available.FirstOrDefault(plugin => string.Equals(plugin.Id, preferredProjectGridCellDisplayPluginId, StringComparison.Ordinal));
            if (configured is not null)
            {
                return configured;
            }
        }

        IProjectGridCellDisplayPlugin? defaultPlugin =
            available.FirstOrDefault(plugin => string.Equals(plugin.Id, DefaultProjectGridCellDisplayPlugin.PluginId, StringComparison.Ordinal));

        return defaultPlugin ?? available[0];
    }

    private ProjectGridCellDisplayContext BuildDisplayContext(ProjectDto project) => new(
                ServiceProvider,
                project,
                project.Stats,
                OnClicked,
                SearchString,
                ProjectChildContent);

    private static RenderFragment DefaultProjectCell(ProjectGridCellDisplayContext context) => builder =>
                                                                                                        {
                                                                                                            builder.OpenComponent<DefaultProjectGridCellDisplay>(0);
                                                                                                            builder.AddAttribute(1, nameof(DefaultProjectGridCellDisplay.Context), context);
                                                                                                            builder.CloseComponent();
                                                                                                        };

    void IDisposable.Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }
}
