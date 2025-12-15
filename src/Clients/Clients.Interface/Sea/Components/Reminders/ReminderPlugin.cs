namespace Vitarish.Clients.Interface.Sea.Components.Reminders;

internal sealed class ReminderPlugin : IEventItemPlugin
{
    private readonly IStringLocalizer localizer;

    public ReminderPlugin(IStringLocalizer localizer)
    {
        this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public string Id => "event-items/reminders";

    public string DisplayName => localizer["Reminders"];

    public string? Description => localizer["GetRemindersByEventAsyncHelpText"];

    public EventItemCreationPlacement Placement => EventItemCreationPlacement.Action;

    public IReadOnlyCollection<Type> WatchedTypes { get; } = new[]
    {
        typeof(EventDto),
        typeof(ReminderDto)
    };

    public bool IsAvailable(EventItemCreationContext context)
        => context is not null;

    public bool IsAvailable(EventTimelineContext context)
        => context is not null;

    public RenderFragment Render(EventItemCreationContext context)
        => builder =>
        {
            builder.OpenComponent<ReminderAction>(0);
            builder.AddAttribute(1, nameof(ReminderAction.Project), context.Project);
            builder.AddAttribute(2, nameof(ReminderAction.Procedure), context.Procedure);
            builder.AddAttribute(3, nameof(ReminderAction.Event), context.Event);
            builder.CloseComponent();
        };

    public async Task<IReadOnlyList<EventTimelineItem>> GetItemsAsync(EventTimelineContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        IRemindersClient remindersService = context.GetRequiredService<IRemindersClient>();
        IReadOnlyList<ReminderDto> reminders = await remindersService.GetByEventAsync(context.Event.Id, cancellationToken);

        return reminders
            .Select(reminder => new EventTimelineItem(
                reminder.RemindAt.ToLocalTime().DateTime,
                builder =>
                {
                    builder.OpenComponent<ReminderDisplay>(0);
                    builder.AddAttribute(1, nameof(ReminderDisplay.Color), context.Color);
                    builder.AddAttribute(2, nameof(ReminderDisplay.Reminder), reminder);
                    builder.AddAttribute(3, nameof(ReminderDisplay.Event), context.Event);
                    builder.AddAttribute(4, nameof(ReminderDisplay.Project), context.Project);
                    builder.AddAttribute(5, nameof(ReminderDisplay.Procedure), context.Procedure);
                    builder.CloseComponent();
                },
                Array.Empty<Guid>()))
            .ToArray();
    }
}
