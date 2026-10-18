using System.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class GrantInitialPlatformAdministratorCommandHandler(
    UsersDbContext dbContext,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    IRequestContext requestContext,
    ISecurityAuditWriter securityAuditWriter) : IRequestHandler<GrantInitialPlatformAdministratorCommand, bool>
{
    public async Task<bool> Handle(GrantInitialPlatformAdministratorCommand request, CancellationToken cancellationToken)
    {
        string? roleId = await dbContext.Roles
            .Where(r => r.Name == IdentityDefinitions.PlatformAdmin)
            .Select(r => r.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (roleId is null || await IsClaimedAsync(roleId, cancellationToken))
            return false;

        if (await activeSessionQuery.HasActiveSessionAsync(request.UserId, cancellationToken))
        {
            await WriteAuditAsync(request, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
            return false;
        }

        // Serializable isolation makes concurrent first sign-ins conflict; the
        // execution strategy retries the loser, which then sees the role claimed.
        bool granted = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;
            if (await IsClaimedAsync(roleId, cancellationToken))
                return false;

            dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = request.UserId, RoleId = roleId });
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return true;
        });

        if (granted)
            await WriteAuditAsync(request, SecurityAuditOutcome.Succeeded, SecurityAuditReasonCatalog.InitialPlatformAdministrator, cancellationToken);
        return granted;
    }

    private Task<bool> IsClaimedAsync(string roleId, CancellationToken cancellationToken) =>
        dbContext.UserRoles.AnyAsync(ur => ur.RoleId == roleId, cancellationToken);

    private async Task WriteAuditAsync(
        GrantInitialPlatformAdministratorCommand request,
        string outcome,
        string reason,
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
                ActorId = request.UserId,
                SubjectId = request.UserId,
                TenantId = requestContext.TenantId ?? UsersSecurityAuditEvents.GlobalScope,
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + request.UserId,
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
