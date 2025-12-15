using System.ComponentModel;

namespace Vitarish.Sea.Tools;

[McpServerToolType]
public static class EventsTools
{
    [McpServerTool(Name = nameof(GetEventsByProcedureAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves events for a procedure.")]
    public static Task<IReadOnlyList<EventDto>> GetEventsByProcedureAsync(
            IEventsClient EventsService,
            Guid procedureId,
            CancellationToken cancellationToken = default) => EventsService.GetByProcedureAsync(procedureId, cancellationToken);

    [McpServerTool(Name = nameof(GetEventAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query gets an event by its identifier.")]
    public static async Task<EventDto?> GetEventAsync(
            IEventsClient EventsService,
            Guid id,
            CancellationToken cancellationToken = default) => (await EventsService.GetAsync(id, cancellationToken)).GetValueOrDefault();

    [McpServerTool(Name = nameof(AddEventAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command creates a new event.")]
    public static Task<EventDto?> AddEventAsync(
            IEventsClient EventsService,
            Guid projectId,
            Guid procedureId,
            CancellationToken cancellationToken = default) => EventsService.CreateEventAsync(projectId, procedureId, null, cancellationToken);

    [McpServerTool(Name = nameof(AddEventAfterAsync), Destructive = false, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command inserts a new event after a specific event.")]
    public static Task<EventDto?> AddEventAfterAsync(
            IEventsClient EventsService,
            Guid projectId,
            Guid procedureId,
            Guid afterEventId,
            CancellationToken cancellationToken = default) => EventsService.CreateEventAsync(projectId, procedureId, afterEventId, cancellationToken);

    [McpServerTool(Name = nameof(UpdateEventAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = true), Description("This command updates an existing event.")]
    public static Task<EventDto?> UpdateEventAsync(
            IEventsClient EventsService,
            Guid id,
            string title,
            string description,
            CancellationToken cancellationToken = default) => EventsService.UpdateEventDetailsAsync(id, title, description, cancellationToken);

    [McpServerTool(Name = nameof(UpdateEventChainAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command updates the chain order of events within a procedure.")]
    public static Task UpdateEventChainAsync(
            IEventsClient EventsService,
            Guid procedureId,
            IReadOnlyList<EventChainChangeDto> chain,
            CancellationToken cancellationToken = default) => EventsService.UpdateChainAsync(procedureId, chain, cancellationToken);

    [McpServerTool(Name = nameof(MoveEventUpAsync), Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command moves an event up within its chain.")]
    public static Task<bool> MoveEventUpAsync(
            IEventsClient EventsService,
            Guid procedureId,
            Guid eventId,
            CancellationToken cancellationToken = default) => EventsService.MoveUpAsync(procedureId, eventId, cancellationToken);

    [McpServerTool(Name = nameof(MoveEventDownAsync), Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command moves an event down within its chain.")]
    public static Task<bool> MoveEventDownAsync(
            IEventsClient EventsService,
            Guid procedureId,
            Guid eventId,
            CancellationToken cancellationToken = default) => EventsService.MoveDownAsync(procedureId, eventId, cancellationToken);

    [McpServerTool(Name = nameof(RemoveEventAsync), Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command removes an event from the chain and repairs links.")]
    public static Task<bool> RemoveEventAsync(
            IEventsClient EventsService,
            Guid procedureId,
            Guid eventId,
            CancellationToken cancellationToken = default) => EventsService.RemoveEventAsync(procedureId, eventId, cancellationToken);

    [McpServerTool(Name = nameof(RepairEventChainAsync), Destructive = true, Idempotent = true, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command repairs a procedure's event chain order using a best-guess approach.")]
    public static Task RepairEventChainAsync(
            IEventsClient EventsService,
            Guid procedureId,
            CancellationToken cancellationToken = default) => EventsService.RepairChainAsync(procedureId, cancellationToken);

    [McpServerTool(Name = nameof(CommitEventAsync), Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command commits an event.")]
    public static Task CommitEventAsync(
            IEventsClient EventsService,
            Guid eventId,
            Guid reporterUserId,
            DateTimeOffset commitTime,
            CancellationToken cancellationToken = default) => EventsService.CommitAsync(eventId, reporterUserId, commitTime, cancellationToken);

    [McpServerTool(Name = nameof(UncommitEventAsync), Destructive = true, Idempotent = false, OpenWorld = false, ReadOnly = false, UseStructuredContent = false), Description("This command uncommits an event.")]
    public static Task UncommitEventAsync(
            IEventsClient EventsService,
            Guid eventId,
            CancellationToken cancellationToken = default) => EventsService.UncommitAsync(eventId, cancellationToken);

    [McpServerTool(Name = nameof(GetEventRemindersCountAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = false), Description("This query gets the number of reminders for an event.")]
    public static Task<int> GetEventRemindersCountAsync(
            IEventsClient EventsService,
            Guid eventId,
            CancellationToken cancellationToken = default) => EventsService.GetRemindersCountAsync(eventId, cancellationToken);

    [McpServerTool(Name = nameof(GetEventSnapshotAsync), Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true, UseStructuredContent = true), Description("This query retrieves a snapshot for the specified event.")]
    public static Task<EventSnapshot?> GetEventSnapshotAsync(
            IEventsClient EventsService,
            Guid id,
            CancellationToken cancellationToken = default) => EventsService.CaptureSnapshotAsync(id, cancellationToken);

}
