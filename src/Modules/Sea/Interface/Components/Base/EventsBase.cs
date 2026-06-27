namespace Vitarish.Sea.Interface.Components.Base;

public class EventsBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<EventReadDto>();
        RenderForType<AttachmentReadDto>();
        RenderForType<ReminderReadDto>();
    }
}
