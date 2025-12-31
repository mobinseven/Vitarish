namespace Vitarish.Sea.Interface.Components.Attachments;

internal sealed class AttachmentPlugin : IEventItemPlugin
{
    private readonly IStringLocalizer localizer;

    public AttachmentPlugin(IStringLocalizer localizer)
    {
        this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public string Id => "event-items/attachments";

    public string DisplayName => localizer["Attachments"];

    public string? Description => localizer["AddAttachmentsAsyncHelpText"];

    public EventItemCreationPlacement Placement => EventItemCreationPlacement.Main;

    public IReadOnlyCollection<Type> WatchedTypes { get; } = new[]
    {
        typeof(EventDto),
        typeof(AttachmentDto)
    };

    public bool IsAvailable(EventItemCreationContext context) => context is not null;

    public bool IsAvailable(EventTimelineContext context) => context is not null;

    public RenderFragment Render(EventItemCreationContext context) => builder =>
                                                                               {
                                                                                   builder.OpenComponent<AttachmentCreator>(0);
                                                                                   builder.AddAttribute(1, nameof(AttachmentCreator.Project), context.Project);
                                                                                   builder.AddAttribute(2, nameof(AttachmentCreator.Procedure), context.Procedure);
                                                                                   builder.AddAttribute(3, nameof(AttachmentCreator.Event), context.Event);
                                                                                   builder.AddAttribute(4, nameof(AttachmentCreator.Actions), context.Actions);
                                                                                   builder.CloseComponent();
                                                                               };

    public async Task<IReadOnlyList<EventTimelineItem>> GetItemsAsync(EventTimelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        IAttachmentsService attachmentsService = context.GetRequiredService<IAttachmentsService>();
        IReadOnlyList<AttachmentDto> attachments = await attachmentsService.GetByEventAsync(context.Event.Id, cancellationToken);

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
