namespace Vitarish.Sea.Interface.Components.Events;

public class EventsBase : SeaCommonBase
{
    [Inject] protected IEventsService EventsService { get; set; } = default!;
    [Inject] protected IAttachmentsService AttachmentsService { get; set; } = default!;
    [Inject] protected IEventRolesService EventRolesService { get; set; } = default!;
    [Inject] protected IRolesService RolesService { get; set; } = default!;
    [Inject] protected IUserRolesService UserRolesService { get; set; } = default!;
    [Inject] protected IReceiptsService ReceiptsService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<EventDto>();
        RenderForType<AttachmentDto>();
        RenderForType<ReminderDto>();
    }
}
