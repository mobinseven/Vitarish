namespace Vitarish.Sea.Interface.Components.UsageConfigurations;

public class UsageConfigurationsBase : SeaCommonBase
{
    [Inject] protected IUsageConfigurationService UsageConfigurationService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UsageConfigurationDto>();
    }
}

