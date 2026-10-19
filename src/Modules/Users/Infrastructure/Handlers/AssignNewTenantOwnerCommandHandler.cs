using System.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class AssignNewTenantOwnerCommandHandler(
    UsersDbContext dbContext,
    IDistributedCache cache,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IRequestHandler<AssignNewTenantOwnerCommand, AssignNewTenantOwnerResult>
{
    public async Task<AssignNewTenantOwnerResult> Handle(AssignNewTenantOwnerCommand request, CancellationToken cancellationToken)
    {
        string ownerRoleName = TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantOwner);
        string[] roleNames =
        [
            ownerRoleName,
            TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantUser)
        ];
        string operationKey = $"{request.TenantId:N}:{request.UserId}";
        // A user makes only themselves the owner of a tenant they are creating.
        if (!string.Equals(requestContext.UserId, request.UserId, StringComparison.Ordinal))
        {
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return AssignNewTenantOwnerResult.Unauthorized;
        }

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => AssignAsync(request, ownerRoleName, roleNames, operationKey, cancellationToken));
    }

    private async Task<AssignNewTenantOwnerResult> AssignAsync(
        AssignNewTenantOwnerCommand request,
        string ownerRoleName,
        string[] roleNames,
        string operationKey,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        User? user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return AssignNewTenantOwnerResult.UserNotFound;
        }

        List<IdentityRole> roles = await dbContext.Roles.Where(role => roleNames.Contains(role.Name!)).ToListAsync(cancellationToken);
        List<string> roleIds = [.. roles.Select(role => role.Id)];
        List<string> memberIds = await dbContext.UserRoles
            .Where(membership => roleIds.Contains(membership.RoleId))
            .Select(membership => membership.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (memberIds.Any(memberId => memberId != user.Id))
        {
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return AssignNewTenantOwnerResult.TenantHasMembers;
        }

        IdentityRole? ownerRole = roles.SingleOrDefault(role => role.Name == ownerRoleName);
        if (ownerRole is not null && memberIds.Contains(user.Id)
            && await dbContext.UserRoles.AnyAsync(membership => membership.UserId == user.Id && membership.RoleId == ownerRole.Id, cancellationToken))
        {
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return AssignNewTenantOwnerResult.AlreadyOwner;
        }

        foreach (string roleName in roleNames.Where(name => roles.All(role => role.Name != name)))
        {
            IdentityRole role = new() { Id = Guid.NewGuid().ToString(), Name = roleName, NormalizedName = roleName.ToUpperInvariant() };
            dbContext.Roles.Add(role);
            roles.Add(role);
        }

        if (await activeSessionQuery.HasActiveSessionAsync(user.Id, cancellationToken))
        {
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
            return AssignNewTenantOwnerResult.ActiveSessionConflict;
        }

        ownerRole = roles.Single(role => role.Name == ownerRoleName);
        dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = ownerRole.Id });
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        await WriteAuditAsync(
            request,
            operationKey,
            SecurityAuditOutcome.Succeeded,
            SecurityAuditReasonCatalog.TenantRoleReplacement(SecurityAuditReasonCatalog.TenantRoleStateNone, SecurityAuditReasonCatalog.TenantRoleStateOwner),
            CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        return AssignNewTenantOwnerResult.Assigned;
    }

    private Task WriteAuditAsync(AssignNewTenantOwnerCommand request, string operationKey, string outcome, string reason, CancellationToken cancellationToken) =>
        NewTenantOwnerAudit.WriteAsync(securityAuditWriter, requestContext, request.UserId, request.TenantId, operationKey, outcome, reason, cancellationToken);
}

internal static class NewTenantOwnerAudit
{
    public static async Task WriteAsync(
        ISecurityAuditWriter securityAuditWriter,
        IRequestContext requestContext,
        string userId,
        Guid tenantId,
        string operationKey,
        string outcome,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = UsersSecurityAuditEvents.TenantRoleReplacement,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = userId,
                TenantId = tenantId.ToString(),
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + userId,
                Operation = UsersSecurityAuditEvents.TenantRoleReplacementOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = operationKey + UsersSecurityAuditEvents.NewTenantOwnerIdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the role result when the mandatory audit store is unavailable.
        }
    }
}

internal sealed class RevokeNewTenantOwnerCommandHandler(
    UsersDbContext dbContext,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IRequestHandler<RevokeNewTenantOwnerCommand>
{
    public async Task Handle(RevokeNewTenantOwnerCommand request, CancellationToken cancellationToken)
    {
        string operationKey = $"{request.TenantId:N}:{request.UserId}:revoke";
        if (!string.Equals(requestContext.UserId, request.UserId, StringComparison.Ordinal))
        {
            await NewTenantOwnerAudit.WriteAsync(securityAuditWriter, requestContext, request.UserId, request.TenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            throw new InvalidOperationException("Only the creator can undo their own new-tenant ownership.");
        }

        string[] roleNames =
        [
            TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantOwner),
            TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantUser)
        ];
        List<IdentityRole> roles = await dbContext.Roles.Where(role => roleNames.Contains(role.Name!)).ToListAsync(cancellationToken);
        List<string> roleIds = [.. roles.Select(role => role.Id)];
        List<IdentityUserRole<string>> memberships = await dbContext.UserRoles.Where(membership => roleIds.Contains(membership.RoleId)).ToListAsync(cancellationToken);
        // Only the creator can hold a role in a tenant whose creation is being undone.
        if (memberships.Any(membership => membership.UserId != request.UserId))
            throw new InvalidOperationException("The tenant has members other than its creator, so its owner assignment cannot be undone.");

        if (memberships.Count == 0 && roles.Count == 0)
            return;

        User? user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
        if (user is not null)
            user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        dbContext.UserRoles.RemoveRange(memberships);
        dbContext.Roles.RemoveRange(roles);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await NewTenantOwnerAudit.WriteAsync(
            securityAuditWriter,
            requestContext,
            request.UserId,
            request.TenantId,
            operationKey,
            SecurityAuditOutcome.Succeeded,
            SecurityAuditReasonCatalog.TenantRoleReplacement(SecurityAuditReasonCatalog.TenantRoleStateOwner, SecurityAuditReasonCatalog.TenantRoleStateNone),
            CancellationToken.None);
    }
}
