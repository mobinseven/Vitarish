using Vitarish.Sea.Interface.Components.Attachments;

namespace Vitarish.Sea.Interface.Plugins;

internal sealed class AttachmentPlugin(IAttachmentsService attachmentsService) : IEventItemPlugin
{
    public string Id => "event-items/attachments";

    public EventItemCreationPlacement Placement => EventItemCreationPlacement.Main;

    public bool IsAvailable(EventTimelineContext context) => context is not null;

    public RenderFragment Render(EventItemCreationContext context) => builder =>
                                                                               {
                                                                                   builder.OpenComponent<AttachmentCreator>(0);
                                                                                   builder.AddAttribute(1, nameof(AttachmentCreator.Project), context.Project);
                                                                                   builder.AddAttribute(2, nameof(AttachmentCreator.Procedure), context.Procedure);
                                                                                   builder.AddAttribute(3, nameof(AttachmentCreator.Event), context.Event);
                                                                                   builder.AddAttribute(4, nameof(AttachmentCreator.Actions), context.Actions);
                                                                                   builder.AddAttribute(5, nameof(AttachmentCreator.OnCreated), EventCallback.Factory.Create(this, context.OnCreated));
                                                                                   builder.CloseComponent();
                                                                               };

    public async Task<IReadOnlyList<EventTimelineItem>> GetItemsAsync(EventTimelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        IReadOnlyList<AttachmentReadDto> attachments = await attachmentsService.GetByEventAsync(context.Event.Id, cancellationToken);

        return [.. attachments
            .Select(attachment => new EventTimelineItem(
                attachment.CreatedAt.LocalDateTime,
                builder =>
                {
                    builder.OpenComponent<AttachmentsDisplay>(0);
                    builder.AddAttribute(1, nameof(AttachmentsDisplay.Event), context.Event);
                    builder.AddAttribute(2, nameof(AttachmentsDisplay.Attachment), attachment);
                    builder.AddAttribute(3, nameof(AttachmentsDisplay.Color), context.Color);
                    builder.CloseComponent();
                },
                []))];
    }
}
