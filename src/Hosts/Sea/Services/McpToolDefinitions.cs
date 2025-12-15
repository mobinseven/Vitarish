
using Vitarish.Sea.Tools;

namespace Vitarish.Sea.Services;

public static class McpToolDefinitions
{
    public static IReadOnlyList<string> QueryToolNames { get; } =
    [
        nameof(AttachmentsTools.GetAttachmentsAsync),
        nameof(AttachmentsTools.GetAttachmentsCountAsync),
        nameof(AttachmentsTools.GetAttachmentSnapshotAsync),
        nameof(EventsTools.GetEventsByProcedureAsync),
        nameof(EventsTools.GetEventAsync),
        nameof(EventsTools.GetEventRemindersCountAsync),
        nameof(EventsTools.GetEventSnapshotAsync),
        nameof(ProceduresTools.GetProceduresAsync),
        nameof(ProceduresTools.GetProcedureSnapshotAsync),
        nameof(ProjectsTools.GetProjectsAsync),
        nameof(ProjectsTools.GetProjectsCountAsync),
        nameof(ProjectsTools.GetProjectAsync),
        nameof(ProjectsTools.GetProjectSnapshotAsync),
        nameof(RemindersTools.GetRemindersByEventAsync),
        nameof(RemindersTools.GetDueRemindersAsync),
        nameof(UsageConfigurationTools.GetUsageConfigurationAsync)
    ];

    public static IReadOnlyList<string> CommandToolNames { get; } =
    [
        nameof(AttachmentsTools.AddAttachmentAsync),
        nameof(AttachmentsTools.AddAttachmentsAsync),
        nameof(AttachmentsTools.RemoveAttachmentAsync),
        nameof(AttachmentsTools.UpdateAttachmentAsync),
        nameof(EventsTools.AddEventAsync),
        nameof(EventsTools.AddEventAfterAsync),
        nameof(EventsTools.UpdateEventAsync),
        nameof(EventsTools.UpdateEventChainAsync),
        nameof(EventsTools.MoveEventUpAsync),
        nameof(EventsTools.MoveEventDownAsync),
        nameof(EventsTools.RemoveEventAsync),
        nameof(EventsTools.RepairEventChainAsync),
        nameof(EventsTools.CommitEventAsync),
        nameof(EventsTools.UncommitEventAsync),
        nameof(ProceduresTools.AddProcedureAsync),
        nameof(ProceduresTools.UpdateProcedureAsync),
        nameof(ProceduresTools.DeleteProcedureAsync),
        nameof(ProjectsTools.AddProjectAsync),
        nameof(ProjectsTools.UpdateProjectAsync),
        nameof(ProjectsTools.RemoveProjectAsync),
        nameof(RemindersTools.AddRemindersAsync),
        nameof(RemindersTools.UpdateReminderAsync),
        nameof(RemindersTools.RemoveReminderAsync),
        nameof(RemindersTools.MarkReminderSentAsync),
        nameof(UsageConfigurationTools.UpdateUsageConfigurationAsync)
    ];
}
