namespace Vitarish.Sea.Interface.Components.Base;

public class RemindersBase : SeaCommonBase
{
    [Inject] protected IRemindersService RemindersService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReminderReadDto>();
    }
}
