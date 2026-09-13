using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Vitarish.Sea.Api.Authorization;

public static class SeaAuthorizationOperations
{
    public static OperationAuthorizationRequirement Read { get; } = new() { Name = nameof(Read) };
    public static OperationAuthorizationRequirement ReadManagement { get; } = new() { Name = nameof(ReadManagement) };
    public static OperationAuthorizationRequirement Write { get; } = new() { Name = nameof(Write) };
    public static OperationAuthorizationRequirement ReadCapabilities { get; } = new() { Name = nameof(ReadCapabilities) };
    public static OperationAuthorizationRequirement CreateProject { get; } = new() { Name = nameof(CreateProject) };
    public static OperationAuthorizationRequirement UpdateProject { get; } = new() { Name = nameof(UpdateProject) };
    public static OperationAuthorizationRequirement DeleteProject { get; } = new() { Name = nameof(DeleteProject) };
    public static OperationAuthorizationRequirement CreateProcedure { get; } = new() { Name = nameof(CreateProcedure) };
    public static OperationAuthorizationRequirement UpdateProcedure { get; } = new() { Name = nameof(UpdateProcedure) };
    public static OperationAuthorizationRequirement DeleteProcedure { get; } = new() { Name = nameof(DeleteProcedure) };
    public static OperationAuthorizationRequirement CreateEvent { get; } = new() { Name = nameof(CreateEvent) };
    public static OperationAuthorizationRequirement UpdateEvent { get; } = new() { Name = nameof(UpdateEvent) };
    public static OperationAuthorizationRequirement DeleteEvent { get; } = new() { Name = nameof(DeleteEvent) };
    public static OperationAuthorizationRequirement MoveEvent { get; } = new() { Name = nameof(MoveEvent) };
    public static OperationAuthorizationRequirement CommitEvent { get; } = new() { Name = nameof(CommitEvent) };
    public static OperationAuthorizationRequirement ListRoles { get; } = new() { Name = nameof(ListRoles) };
    public static OperationAuthorizationRequirement CreateRole { get; } = new() { Name = nameof(CreateRole) };
    public static OperationAuthorizationRequirement UpdateRole { get; } = new() { Name = nameof(UpdateRole) };
    public static OperationAuthorizationRequirement DeleteRole { get; } = new() { Name = nameof(DeleteRole) };
    public static OperationAuthorizationRequirement ListUserRoles { get; } = new() { Name = nameof(ListUserRoles) };
    public static OperationAuthorizationRequirement AssignUserRole { get; } = new() { Name = nameof(AssignUserRole) };
    public static OperationAuthorizationRequirement RemoveUserRole { get; } = new() { Name = nameof(RemoveUserRole) };
    public static OperationAuthorizationRequirement ListEventRoles { get; } = new() { Name = nameof(ListEventRoles) };
    public static OperationAuthorizationRequirement ListEventReadRoles { get; } = new() { Name = nameof(ListEventReadRoles) };
    public static OperationAuthorizationRequirement ListEventWriteRoles { get; } = new() { Name = nameof(ListEventWriteRoles) };
    public static OperationAuthorizationRequirement AssignEventRole { get; } = new() { Name = nameof(AssignEventRole) };
    public static OperationAuthorizationRequirement RemoveEventRole { get; } = new() { Name = nameof(RemoveEventRole) };
    public static OperationAuthorizationRequirement ReadStatistics { get; } = new() { Name = nameof(ReadStatistics) };

    private static IReadOnlySet<string> ManagementOperationNames { get; } = new HashSet<string>(
    [
        ReadManagement.Name,
        ReadCapabilities.Name,
        CreateProject.Name,
        UpdateProject.Name,
        DeleteProject.Name,
        CreateProcedure.Name,
        UpdateProcedure.Name,
        DeleteProcedure.Name,
        CreateEvent.Name,
        UpdateEvent.Name,
        DeleteEvent.Name,
        MoveEvent.Name,
        CommitEvent.Name,
        ListRoles.Name,
        CreateRole.Name,
        UpdateRole.Name,
        DeleteRole.Name,
        ListUserRoles.Name,
        AssignUserRole.Name,
        RemoveUserRole.Name,
        ListEventRoles.Name,
        ListEventReadRoles.Name,
        ListEventWriteRoles.Name,
        AssignEventRole.Name,
        RemoveEventRole.Name,
        ReadStatistics.Name
    ], StringComparer.Ordinal);

    public static bool IsManagementOperation(string? name) =>
        name is not null && ManagementOperationNames.Contains(name);
}
