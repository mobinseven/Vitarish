namespace Vitarish.Clients.Interface.Sea.Components.UsageConfigurations;

public partial class UsageConfigurationEditor : UsageConfigurationsBase
{
    private const string AutomaticPluginSelectionValue = "__automatic";

    private static readonly IReadOnlyList<ModelOption> AvailableModels =
    [
        new("gpt-5-2025-08-07", "GPT-5"),
        new("gpt-5-mini-2025-08-07", "GPT-5 Mini"),
        new("gpt-5-nano-2025-08-07", "GPT-5 Nano"),
        new("o4-mini-deep-research-2025-06-26", "O4 Mini Deep Research"),
        new("o3-deep-research-2025-06-26", "O3 Deep Research"),
        new("gpt-4.1-2025-04-14", "GPT-4.1"),
        new("gpt-4.1-nano-2025-04-14", "GPT-4.1 Nano")
    ];

    private UsageConfigurationDto _configuration = new(Guid.Empty, null, "{}");
    private UsageConfigurationMetadata _metadata = new();
    private List<IProjectGridCellDisplayPlugin> _projectGridCellDisplayPlugins = [];
    private bool _clinicPatientIntakePluginAvailable;

    [Inject] private IEnumerable<IProjectGridCellDisplayPlugin> ProjectGridCellDisplayPlugins { get; set; } = default!;
    [Inject] private ILoadedPluginCatalog LoadedPluginCatalog { get; set; } = default!;
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    private string PreferredProjectGridCellDisplayPluginId
    {
        get => _metadata.ProjectGridCellDisplayPluginId ?? AutomaticPluginSelectionValue;
        set => _metadata.ProjectGridCellDisplayPluginId = value == AutomaticPluginSelectionValue ? null : value;
    }

    protected override async Task OnInitializedAsync()
    {
        _configuration = await UsageConfigurationService.GetAsync(CancellationToken.None)
            ?? new(Guid.Empty, null, "{}");
        _metadata = UsageConfigurationMetadata.FromJson(_configuration.Metadata);
        if (string.IsNullOrWhiteSpace(_metadata.ThemeSettingsJson))
        {
            _metadata.ThemeSettingsJson = ThemeService.GetThemeSettingsJson();
        }
        else
        {
            ThemeService.LoadThemeSettingsJson(_metadata.ThemeSettingsJson);
        }
        _projectGridCellDisplayPlugins =
            ProjectGridCellDisplayPlugins
                .OrderBy(plugin => plugin.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        _clinicPatientIntakePluginAvailable = LoadedPluginCatalog.IsLoaded(SeaPluginIdentifiers.Clinic);
    }

    private Task SaveAsync()
    {
        _metadata.ChatInstructions = Normalize(_metadata.ChatInstructions);
        _metadata.ProjectOverviewInstructions = Normalize(_metadata.ProjectOverviewInstructions);
        _metadata.ProcedureOverviewInstructions = Normalize(_metadata.ProcedureOverviewInstructions);

        _metadata.ChatModel = Normalize(_metadata.ChatModel);

        if (_metadata.ChatModel is { Length: > 0 } model && !AvailableModels.Any(option => option.Id == model))
        {
            _metadata.ChatModel = null;
        }
        _metadata.ThemeSettingsJson = ThemeService.GetThemeSettingsJson();
        _configuration.Metadata = _metadata.ToJson();
        return UsageConfigurationService.UpdateUsageConfigurationAsync(_configuration, CancellationToken.None);
    }

    private sealed record ModelOption(string Id, string DisplayName);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
