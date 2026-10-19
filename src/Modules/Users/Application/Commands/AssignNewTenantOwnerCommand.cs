using MediatR;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Creates a just-created tenant's roles and makes <paramref name="UserId"/> its only owner.
/// Unlike <see cref="ReplaceTenantRoleCommand"/>, it needs no administrator: only the signed-in
/// user can make themselves the owner, and the tenant-creation use case owns the decision that
/// they may create the tenant. Like every role assignment, it is refused while the user has an
/// active session. Repeating it for the same user and tenant succeeds without change.
/// </summary>
public sealed record AssignNewTenantOwnerCommand(string UserId, Guid TenantId) : IRequest<AssignNewTenantOwnerResult>;

public enum AssignNewTenantOwnerResult
{
    Assigned = 1,
    AlreadyOwner = 2,
    UserNotFound = 3,
    /// <summary>Someone else already holds a role in the tenant, so it is not new.</summary>
    TenantHasMembers = 4,
    ActiveSessionConflict = 5,
    Unauthorized = 6
}

/// <summary>
/// Undoes <see cref="AssignNewTenantOwnerCommand"/> when tenant creation fails after it ran:
/// removes the tenant's role memberships and roles, and audits it. Only the signed-in creator
/// can undo their own ownership, and only while nobody else holds a role in the tenant.
/// </summary>
public sealed record RevokeNewTenantOwnerCommand(string UserId, Guid TenantId) : IRequest;
