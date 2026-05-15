namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Sea
    {
        public const string Dashboard = "/sea";
        public const string OwnerDashboard = "/sea/owner";
        public const string OwnerSettings = "/sea/owner/settings";
        public const string OwnerSettingsUsers = "/sea/owner/settings/users";
        public const string OwnerSettingsAdvanced = "/sea/owner/settings/advanced";
        public const string OwnerSettingsStatistics = "/sea/owner/settings/statistics";

        public const string AdminDashboard = "/sea/admin";
        public const string AdminProjectsSettings = "/sea/admin/settings/projects";
        public const string AdminReminders = "/sea/admin/reminders";

        public const string Projects = "/sea/projects";
        public const string ProjectNew = "/sea/projects/new";
        public const string ProjectTemplate = "/sea/projects/{projectId:guid}";
        public const string ProjectEditTemplate = "/sea/projects/{projectId:guid}/edit";
        public const string ProcedureTemplate = "/sea/projects/{projectId:guid}/procedures/{procedureId:guid}";
        public const string EventTemplate = "/sea/projects/{projectId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public const string TemplateProjects = "/sea/admin/settings/projects/templates";
        public const string TemplateProjectNew = "/sea/admin/settings/projects/templates/new";
        public const string TemplateProjectTemplate = "/sea/admin/settings/projects/templates/{projectId:guid}";
        public const string TemplateProjectEditTemplate = "/sea/admin/settings/projects/templates/{projectId:guid}/edit";
        public const string TemplateProcedureTemplate = "/sea/admin/settings/projects/templates/{projectId:guid}/procedures/{procedureId:guid}";
        public const string TemplateEventTemplate = "/sea/admin/settings/projects/templates/{projectId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public static string Project(Guid projectId) => $"{Projects}/{GuidValue(projectId)}";
        public static string ProjectEdit(Guid projectId) => $"{Project(projectId)}/edit";
        public static string Procedure(Guid projectId, Guid procedureId) =>
            $"{Project(projectId)}/procedures/{GuidValue(procedureId)}";
        public static string Event(Guid projectId, Guid procedureId, Guid eventId) =>
            $"{Procedure(projectId, procedureId)}/events/{GuidValue(eventId)}";

        public static string TemplateProject(Guid projectId) => $"{TemplateProjects}/{GuidValue(projectId)}";
        public static string TemplateProjectEdit(Guid projectId) => $"{TemplateProject(projectId)}/edit";
        public static string TemplateProcedure(Guid projectId, Guid procedureId) =>
            $"{TemplateProject(projectId)}/procedures/{GuidValue(procedureId)}";
        public static string TemplateEvent(Guid projectId, Guid procedureId, Guid eventId) =>
            $"{TemplateProcedure(projectId, procedureId)}/events/{GuidValue(eventId)}";
    }
}
