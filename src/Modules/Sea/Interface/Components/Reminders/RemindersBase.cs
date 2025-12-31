namespace Vitarish.Sea.Interface.Components.Reminders;

public class RemindersBase : SeaCommonBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IRemindersService RemindersService { get; set; } = default!;
    [Inject] protected IUsageConfigurationService UsageConfigurationService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReminderDto>();
    }
}
