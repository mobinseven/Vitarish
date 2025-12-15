namespace Vitarish.Clients.Interface.Sea.Components.UsageConfigurations;

public class UsageConfigurationsBase : SeaCommonBase
{
    [Inject] protected IUsageConfigurationClient UsageConfigurationService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UsageConfigurationDto>();
    }
}

