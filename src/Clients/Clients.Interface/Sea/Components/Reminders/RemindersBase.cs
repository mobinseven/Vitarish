namespace Vitarish.Clients.Interface.Sea.Components.Reminders;

public class RemindersBase : SeaCommonBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected IRemindersClient RemindersService { get; set; } = default!;
    [Inject] protected IUsageConfigurationClient UsageConfigurationService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReminderDto>();
    }
}
