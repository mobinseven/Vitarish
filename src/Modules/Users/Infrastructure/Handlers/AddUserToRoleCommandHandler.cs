using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Contracts.Responses;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class AddUserToRoleCommandHandler(
    UsersDbContext dbContext,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    IRequestContext requestContext,
    ISecurityAuditWriter securityAuditWriter) : IRequestHandler<AddUserToRoleCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(AddUserToRoleCommand request, CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanAssignGlobalRole(requestContext.User))
        {
            IdentityResult result = IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.Unauthorized,
                Description = UsersCommandErrorCodes.UnauthorizedDescription
            });
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return result;
        }

        if (request.TenantId.HasValue || TenantRoleDefinitions.IsTenantRoleName(request.RoleName) || TenantRoleDefinitions.IsTenantRoleSuffix(request.RoleName))
        {
            IdentityResult result = IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.InvalidTenantRole,
                Description = UsersCommandErrorCodes.InvalidTenantRoleDescription
            });
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.InvalidTenantRole, cancellationToken);
            return result;
        }

        if (!TenantRoleDefinitions.PermittedGlobalRoles.Contains(request.RoleName))
        {
            IdentityResult result = IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.InvalidGlobalRole,
                Description = UsersCommandErrorCodes.InvalidGlobalRoleDescription
            });
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return result;
        }

        string roleName = request.RoleName;
        var role = await dbContext.Roles
            .Where(r => r.Name == roleName)
            .Select(r => new { r.Id })
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            IdentityResult result = IdentityResult.Failed(
                new IdentityError
                {
                    Code = UsersCommandErrorCodes.RoleNotFound,
                    Description = UsersCommandErrorCodes.RoleNotFoundDescription
                });
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.GlobalRoleNotFound, cancellationToken);
            return result;
        }

        bool assigned = await dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == request.User.Id && ur.RoleId == role.Id, cancellationToken);
        if (assigned)
        {
            await WriteAuditAsync(request, SecurityAuditOutcome.Succeeded, null, cancellationToken);
            return IdentityResult.Success;
        }

        if (await activeSessionQuery.HasActiveSessionAsync(request.User.Id, cancellationToken))
        {
            IdentityResult result = IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.ActiveSessionConflict,
                Description = UsersCommandErrorCodes.ActiveSessionConflictDescription
            });
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
            return result;
        }

        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = request.User.Id,
            RoleId = role.Id
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(request, SecurityAuditOutcome.Succeeded, null, cancellationToken);
        return IdentityResult.Success;
    }

    private async Task WriteAuditAsync(
        AddUserToRoleCommand request,
        string outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        string key = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = UsersSecurityAuditEvents.GlobalRoleAssignment,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = request.User.Id,
                TenantId = requestContext.TenantId ?? UsersSecurityAuditEvents.GlobalScope,
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + request.User.Id,
                Operation = UsersSecurityAuditEvents.GlobalRoleAssignmentOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? key,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = key + UsersSecurityAuditEvents.GlobalRoleAssignmentIdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the global-role result when the audit store is unavailable.
        }
    }
}
