namespace Vitarish.Sea.Interface.Components.Events;

public class EventsBase : SeaCommonBase
{
    [Inject] protected IAccessControlledEventsService EventsService { get; set; } = default!;
    [Inject] protected IEventRolesService EventRolesService { get; set; } = default!;
    [Inject] protected IRolesService RolesService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<EventDto>();
        RenderForType<AttachmentDto>();
        RenderForType<ReminderDto>();
    }
}
