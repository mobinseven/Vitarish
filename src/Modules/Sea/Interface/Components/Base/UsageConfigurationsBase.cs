namespace Vitarish.Sea.Interface.Components.Base;

public class UsageConfigurationsBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<UsageConfigurationDto>();
    }
}

