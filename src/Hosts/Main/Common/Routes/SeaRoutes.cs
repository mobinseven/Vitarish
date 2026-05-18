namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Sea
    {
        public const string Dashboard = "/sea";

        public const string OwnerDashboard = "/owner/sea";
        public const string OwnerSettings = "/owner/sea/settings";
        public const string OwnerSettingsUsers = "/owner/sea/settings/users";
        public const string OwnerSettingsAdvanced = "/owner/sea/settings/advanced";
        public const string OwnerSettingsStatistics = "/owner/sea/settings/statistics";
        public const string OwnerProjectsSettings = "/owner/sea/settings/projects";
        public const string OwnerReminders = "/owner/sea/reminders";

        public const string OwnerTemplateProjects = "/owner/sea/settings/projects/templates";
        public const string OwnerTemplateProjectNew = "/owner/sea/settings/projects/templates/new";
        public const string OwnerTemplateProjectTemplate = "/owner/sea/settings/projects/templates/{projectId:guid}";
        public const string OwnerTemplateProjectEditTemplate = "/owner/sea/settings/projects/templates/{projectId:guid}/edit";
        public const string OwnerTemplateProcedureTemplate = "/owner/sea/settings/projects/templates/{projectId:guid}/procedures/{procedureId:guid}";
        public const string OwnerTemplateEventTemplate = "/owner/sea/settings/projects/templates/{projectId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public const string Projects = "/user/sea/projects";
        public const string ProjectNew = "/user/sea/projects/new";
        public const string ProjectTemplate = "/user/sea/projects/{projectId:guid}";
        public const string ProjectEditTemplate = "/user/sea/projects/{projectId:guid}/edit";
        public const string ProcedureTemplate = "/user/sea/projects/{projectId:guid}/procedures/{procedureId:guid}";
        public const string EventTemplate = "/user/sea/projects/{projectId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public static string Project(Guid projectId) => $"{Projects}/{GuidValue(projectId)}";
        public static string ProjectEdit(Guid projectId) => $"{Project(projectId)}/edit";
        public static string Procedure(Guid projectId, Guid procedureId) =>
            $"{Project(projectId)}/procedures/{GuidValue(procedureId)}";
        public static string Event(Guid projectId, Guid procedureId, Guid eventId) =>
            $"{Procedure(projectId, procedureId)}/events/{GuidValue(eventId)}";

        public static string TemplateProject(Guid projectId) => $"{OwnerTemplateProjects}/{GuidValue(projectId)}";
        public static string TemplateProjectEdit(Guid projectId) => $"{TemplateProject(projectId)}/edit";
        public static string TemplateProcedure(Guid projectId, Guid procedureId) =>
            $"{TemplateProject(projectId)}/procedures/{GuidValue(procedureId)}";
        public static string TemplateEvent(Guid projectId, Guid procedureId, Guid eventId) =>
            $"{TemplateProcedure(projectId, procedureId)}/events/{GuidValue(eventId)}";
    }
}
