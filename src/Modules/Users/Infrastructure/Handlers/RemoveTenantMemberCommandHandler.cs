using System.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class RemoveTenantMemberCommandHandler(
    UsersDbContext dbContext,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IRequestHandler<RemoveTenantMemberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(RemoveTenantMemberCommand request, CancellationToken cancellationToken)
    {
        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) ||
            !UsersSecurityAuthorization.CanManageTenantMembers(requestContext.User, tenantId))
        {
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return Failed(UsersCommandErrorCodes.Unauthorized, UsersCommandErrorCodes.UnauthorizedDescription);
        }

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            string ownerRoleName = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner);
            IdentityRole? ownerRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == ownerRoleName, cancellationToken);
            if (ownerRole is not null && await dbContext.UserRoles.AnyAsync(membership => membership.UserId == request.UserId && membership.RoleId == ownerRole.Id, cancellationToken))
            {
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
                return Failed(UsersCommandErrorCodes.TenantOwnerRemovalRejected, UsersCommandErrorCodes.TenantOwnerRemovalRejectedDescription);
            }

            IdentityRole? memberRole = await dbContext.Roles.SingleOrDefaultAsync(
                role => role.Name == TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser), cancellationToken);
            if (memberRole is null)
            {
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.TenantRoleNotFound, cancellationToken);
                return Failed(UsersCommandErrorCodes.RoleNotFound, UsersCommandErrorCodes.RoleNotFoundDescription);
            }

            IdentityUserRole<string>? membership = await dbContext.UserRoles.SingleOrDefaultAsync(
                value => value.UserId == request.UserId && value.RoleId == memberRole.Id, cancellationToken);
            if (membership is null)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Succeeded, null, cancellationToken);
                return IdentityResult.Success;
            }

            if (await activeSessionQuery.HasActiveSessionAsync(request.UserId, cancellationToken))
            {
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
                return Failed(UsersCommandErrorCodes.ActiveSessionConflict, UsersCommandErrorCodes.ActiveSessionConflictDescription);
            }

            dbContext.UserRoles.Remove(membership);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            await WriteAuditAsync(
                request,
                tenantId,
                operationKey,
                SecurityAuditOutcome.Succeeded,
                null,
                cancellationToken,
                [new SecurityAuditChange(
                    SecurityAuditChangeFields.TenantRole,
                    SecurityAuditReasonCatalog.TenantRoleStateUser,
                    SecurityAuditReasonCatalog.TenantRoleStateNone)]);
            return IdentityResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.TenantRoleConcurrencyConflict, cancellationToken);
            return Failed(UsersCommandErrorCodes.TenantMemberConflict, UsersCommandErrorCodes.TenantMemberConflictDescription);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            dbContext.ChangeTracker.Clear();
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.TenantRolePersistenceFailure, CancellationToken.None);
            throw;
        }
    }

    private async Task WriteAuditAsync(
        RemoveTenantMemberCommand request,
        Guid tenantId,
        string operationKey,
        string outcome,
        string? reason,
        CancellationToken cancellationToken,
        IReadOnlyList<SecurityAuditChange>? changes = null)
    {
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = UsersSecurityAuditEvents.TenantMemberRemoval,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = request.UserId,
                TenantId = tenantId == Guid.Empty ? requestContext.TenantId ?? UsersSecurityAuditEvents.GlobalScope : tenantId.ToString(),
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + request.UserId,
                Operation = UsersSecurityAuditEvents.TenantMemberRemovalOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                Changes = changes,
                IdempotencyKey = operationKey + UsersSecurityAuditEvents.TenantMemberRemovalIdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
        }
    }

    private static IdentityResult Failed(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });
}
