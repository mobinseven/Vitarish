using System.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class ReplaceTenantRoleCommandHandler(
    UsersDbContext dbContext,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IRequestHandler<ReplaceTenantRoleCommand, IdentityResult>
{
    private const int MaxConcurrencyAttempts = 3;

    public async Task<IdentityResult> Handle(ReplaceTenantRoleCommand request, CancellationToken cancellationToken)
    {
        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");

        if (!UsersSecurityAuthorization.CanReplaceTenantRole(requestContext.User))
        {
            IdentityResult result = Failed(UsersCommandErrorCodes.Unauthorized, UsersCommandErrorCodes.UnauthorizedDescription);
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return result;
        }

        if (request.RoleSuffix is not null && !TenantRoleDefinitions.IsPermittedRoleSuffix(request.RoleSuffix))
        {
            IdentityResult result = Failed(UsersCommandErrorCodes.InvalidTenantRole, UsersCommandErrorCodes.InvalidTenantRoleDescription);
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.InvalidTenantRole, cancellationToken);
            return result;
        }

        if (await activeSessionQuery.HasActiveSessionAsync(request.UserId, cancellationToken))
        {
            IdentityResult result = Failed(UsersCommandErrorCodes.ActiveSessionConflict, UsersCommandErrorCodes.ActiveSessionConflictDescription);
            await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
            return result;
        }

        for (int attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            try
            {
                ReplacementResult replacement = await ExecuteReplacementAsync(request, cancellationToken);
                if (!replacement.Result.Succeeded)
                {
                    string reason = replacement.Result.Errors.Any(error => error.Code == UsersCommandErrorCodes.UserNotFound)
                        ? SecurityAuditReasonCatalog.UserNotFound
                        : replacement.Result.Errors.Any(error => error.Code == UsersCommandErrorCodes.RoleNotFound)
                            ? SecurityAuditReasonCatalog.TenantRoleNotFound
                            : SecurityAuditReasonCatalog.MutationRejected;
                    await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, reason, cancellationToken);
                    return replacement.Result;
                }

                await WriteAuditAsync(
                    request,
                    operationKey,
                    SecurityAuditOutcome.Succeeded,
                    SecurityAuditReasonCatalog.TenantRoleReplacement(replacement.BeforeState, replacement.AfterState),
                    cancellationToken);
                return replacement.Result;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyAttempts)
            {
                dbContext.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                dbContext.ChangeTracker.Clear();
                IdentityResult result = Failed(UsersCommandErrorCodes.TenantRoleConflict, UsersCommandErrorCodes.TenantRoleConflictDescription);
                await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.TenantRoleConcurrencyConflict, cancellationToken);
                return result;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                dbContext.ChangeTracker.Clear();
                await WriteAuditAsync(request, operationKey, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.TenantRolePersistenceFailure, CancellationToken.None);
                throw;
            }
        }

        throw new InvalidOperationException("Tenant role replacement did not produce an outcome.");
    }

    private async Task<ReplacementResult> ExecuteReplacementAsync(ReplaceTenantRoleCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(
            async () => await ReplaceCoreAsync(request, cancellationToken));
    }

    private async Task<ReplacementResult> ReplaceCoreAsync(ReplaceTenantRoleCommand request, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            User? user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken);
            if (user is null)
            {
                return new ReplacementResult(
                    Failed(UsersCommandErrorCodes.UserNotFound, UsersCommandErrorCodes.UserNotFoundDescription),
                    SecurityAuditReasonCatalog.TenantRoleStateNone,
                    request.RoleSuffix is null ? SecurityAuditReasonCatalog.TenantRoleStateNone : TenantRoleState.For(request.RoleSuffix));
            }

            string ownerRoleName = TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantOwner);
            string userRoleName = TenantRoleDefinitions.BuildRoleName(request.TenantId, IdentityDefinitions.TenantUser);
            string? requestedRoleName = request.RoleSuffix is null
                ? null
                : TenantRoleDefinitions.BuildRoleName(request.TenantId, request.RoleSuffix);

            List<IdentityRole> tenantRoles = await dbContext.Roles
                .Where(role => role.Name == ownerRoleName || role.Name == userRoleName)
                .ToListAsync(cancellationToken);
            Dictionary<string, string> tenantRoleNames = tenantRoles.ToDictionary(role => role.Id, role => role.Name!);
            List<IdentityUserRole<string>> memberships = await dbContext.UserRoles
                .Where(userRole => userRole.UserId == request.UserId && tenantRoleNames.Keys.Contains(userRole.RoleId))
                .ToListAsync(cancellationToken);

            string beforeState = TenantRoleState.FromRoleNames(memberships.Select(membership => tenantRoleNames[membership.RoleId]));
            IdentityRole? requestedRole = requestedRoleName is null
                ? null
                : tenantRoles.SingleOrDefault(role => role.Name == requestedRoleName);
            if (request.RoleSuffix is not null && requestedRole is null)
            {
                return new ReplacementResult(
                    Failed(UsersCommandErrorCodes.RoleNotFound, UsersCommandErrorCodes.RoleNotFoundDescription),
                    beforeState,
                    TenantRoleState.For(request.RoleSuffix));
            }

            bool changed = memberships.Count != (requestedRole is null ? 0 : 1) ||
                           requestedRole is not null && memberships.Any(membership => membership.RoleId != requestedRole.Id);
            if (changed)
            {
                dbContext.UserRoles.RemoveRange(memberships);

                if (requestedRole is not null)
                {
                    dbContext.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = request.UserId,
                        RoleId = requestedRole.Id
                    });
                }

                user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return new ReplacementResult(
                IdentityResult.Success,
                beforeState,
                request.RoleSuffix is null ? SecurityAuditReasonCatalog.TenantRoleStateNone : TenantRoleState.For(request.RoleSuffix));
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task WriteAuditAsync(
        ReplaceTenantRoleCommand request,
        string operationKey,
        string outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
            throw new InvalidOperationException("A tenant context is required for tenant-role auditing.");

        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = UsersSecurityAuditEvents.TenantRoleReplacement,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = request.UserId,
                TenantId = requestContext.TenantId,
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + request.UserId,
                Operation = UsersSecurityAuditEvents.TenantRoleReplacementOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = operationKey + UsersSecurityAuditEvents.IdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the role result when the mandatory audit store is unavailable.
        }
    }

    private static IdentityResult Failed(string code, string description) =>
        IdentityResult.Failed(new IdentityError { Code = code, Description = description });

    private sealed record ReplacementResult(IdentityResult Result, string BeforeState, string AfterState);

    private static class TenantRoleState
    {
        public static string For(string roleSuffix) => roleSuffix == IdentityDefinitions.TenantOwner
            ? SecurityAuditReasonCatalog.TenantRoleStateOwner
            : SecurityAuditReasonCatalog.TenantRoleStateUser;

        public static string FromRoleNames(IEnumerable<string> roleNames)
        {
            string[] names = [.. roleNames];
            return names.Length switch
            {
                0 => SecurityAuditReasonCatalog.TenantRoleStateNone,
                1 when names[0].EndsWith($"{TenantRoleDefinitions.RoleSeparator}{IdentityDefinitions.TenantOwner}", StringComparison.Ordinal) => For(IdentityDefinitions.TenantOwner),
                1 when names[0].EndsWith($"{TenantRoleDefinitions.RoleSeparator}{IdentityDefinitions.TenantUser}", StringComparison.Ordinal) => For(IdentityDefinitions.TenantUser),
                _ => SecurityAuditReasonCatalog.TenantRoleStateMultiple
            };
        }
    }
}
