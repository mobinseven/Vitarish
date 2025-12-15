using System.ComponentModel;

namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class UsageConfigurationTools
{
    [McpServerTool(Name = nameof(GetUsageConfigurationAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves the current usage configuration.")]
    public static Task<UsageConfigurationDto?> GetUsageConfigurationAsync(
            IUsageConfigurationClient usageConfigurationService,
            CancellationToken cancellationToken = default) => usageConfigurationService.GetAsync(cancellationToken);

    [McpServerTool(Name = nameof(UpdateUsageConfigurationAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command updates the usage configuration.")]
    public static Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(
            IUsageConfigurationClient usageConfigurationService,
            UsageConfigurationDto usageConfigurationDto,
            CancellationToken cancellationToken = default) => usageConfigurationService.UpdateUsageConfigurationAsync(usageConfigurationDto, cancellationToken);
}
