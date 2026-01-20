using Vitarish.Sea.Interface.Models;

namespace Vitarish.Sea.Interface.Components.UsageConfigurations;

public partial class UsageConfigurationEditor : UsageConfigurationsBase
{
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

    private UsageConfigurationDto? _configuration => UsageConfiguration;
    private UsageConfigurationMetadata? _metadata => UsageConfigurationMetadata;

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
        _configuration.Metadata = _metadata.ToJson();
        return UsageConfigurationService.UpdateAsync(_configuration, CancellationToken.None);
    }

    private sealed record ModelOption(string Id, string DisplayName);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
