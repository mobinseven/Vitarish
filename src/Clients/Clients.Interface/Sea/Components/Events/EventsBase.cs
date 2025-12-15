namespace Vitarish.Clients.Interface.Sea.Components.Events;

public class EventsBase : SeaCommonBase
{
    [Inject] protected IEventsClient EventsService { get; set; } = default!;
    [Inject] protected IAttachmentsClient AttachmentsService { get; set; } = default!;
    [Inject] protected IEventRolesClient EventRolesService { get; set; } = default!;
    [Inject] protected IRolesClient RolesService { get; set; } = default!;
    [Inject] protected IUserRolesClient UserRolesService { get; set; } = default!;
    [Inject] protected IReceiptsClient ReceiptsService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<EventDto>();
        RenderForType<AttachmentDto>();
        RenderForType<ReminderDto>();
    }
}
