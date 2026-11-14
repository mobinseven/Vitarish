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

        RemovalOutcome outcome;
        try
        {
            // A retrying execution strategy refuses a user-initiated transaction unless the strategy runs it.
            IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
            outcome = await strategy.ExecuteAsync(async () => await RemoveCoreAsync(request, tenantId, cancellationToken));
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            outcome = new(
                Failed(UsersCommandErrorCodes.TenantMemberConflict, UsersCommandErrorCodes.TenantMemberConflictDescription),
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.TenantRoleConcurrencyConflict);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            dbContext.ChangeTracker.Clear();
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.TenantRolePersistenceFailure, CancellationToken.None);
            throw;
        }

        await WriteAuditAsync(request, tenantId, operationKey, outcome.AuditOutcome, outcome.Reason, cancellationToken, outcome.Changes);
        return outcome.Result;
    }

    /// <summary>
    /// One attempt of the removal in its own transaction. The execution strategy may run it again after a
    /// transient failure, so it only decides the outcome and leaves auditing to the caller.
    /// </summary>
    private async Task<RemovalOutcome> RemoveCoreAsync(RemoveTenantMemberCommand request, Guid tenantId, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        string ownerRoleName = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner);
        IdentityRole? ownerRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == ownerRoleName, cancellationToken);
        if (ownerRole is not null && await dbContext.UserRoles.AnyAsync(membership => membership.UserId == request.UserId && membership.RoleId == ownerRole.Id, cancellationToken))
        {
            return new(
                Failed(UsersCommandErrorCodes.TenantOwnerRemovalRejected, UsersCommandErrorCodes.TenantOwnerRemovalRejectedDescription),
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.MutationRejected);
        }

        IdentityRole? memberRole = await dbContext.Roles.SingleOrDefaultAsync(
            role => role.Name == TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser), cancellationToken);
        if (memberRole is null)
        {
            return new(
                Failed(UsersCommandErrorCodes.RoleNotFound, UsersCommandErrorCodes.RoleNotFoundDescription),
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.TenantRoleNotFound);
        }

        IdentityUserRole<string>? membership = await dbContext.UserRoles.SingleOrDefaultAsync(
            value => value.UserId == request.UserId && value.RoleId == memberRole.Id, cancellationToken);
        if (membership is null)
        {
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return new(IdentityResult.Success, SecurityAuditOutcome.Succeeded, null);
        }

        if (await activeSessionQuery.HasActiveSessionAsync(request.UserId, cancellationToken))
        {
            return new(
                Failed(UsersCommandErrorCodes.ActiveSessionConflict, UsersCommandErrorCodes.ActiveSessionConflictDescription),
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.ActiveSessionConflict);
        }

        dbContext.UserRoles.Remove(membership);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return new(
            IdentityResult.Success,
            SecurityAuditOutcome.Succeeded,
            null,
            [new SecurityAuditChange(
                SecurityAuditChangeFields.TenantRole,
                SecurityAuditReasonCatalog.TenantRoleStateUser,
                SecurityAuditReasonCatalog.TenantRoleStateNone)]);
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

    private sealed record RemovalOutcome(
        IdentityResult Result,
        string AuditOutcome,
        string? Reason,
        IReadOnlyList<SecurityAuditChange>? Changes = null);
}
