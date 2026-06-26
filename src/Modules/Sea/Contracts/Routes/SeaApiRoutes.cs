namespace Vitarish.Sea.Contracts.Routes;

public static class SeaApiRoutes
{
    public const string Base = "/api/modules/sea";
    public const string Health = "/health";
    public const string Projects = "/projects";
    public const string ProjectById = Projects + "/{id:guid}";
    public const string Procedures = "/procedures";
    public const string ProcedureById = Procedures + "/{id:guid}";
    public const string Events = "/events";
    public const string EventById = Events + "/{id:guid}";
    public const string Attachments = "/attachments";
    public const string AttachmentById = Attachments + "/{id:guid}";
    public const string Roles = "/roles";
    public const string RoleById = Roles + "/{id:guid}";
    public const string UserRoles = "/user-roles";
    public const string EventRoles = "/event-roles";
    public const string Receipts = "/receipts";
    public const string Statistics = "/statistics";
    public const string Snapshots = "/snapshots";
    public const string Metadata = "/metadata";
    public const string Progress = "/progress";
}
