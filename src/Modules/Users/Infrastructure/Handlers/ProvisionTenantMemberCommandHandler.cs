using System.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class ProvisionTenantMemberCommandHandler(
    UsersDbContext dbContext,
    UserManager<User> userManager,
    IDistributedCache cache,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext) : IRequestHandler<ProvisionTenantMemberCommand, ProvisionTenantMemberResult>
{
    public async Task<ProvisionTenantMemberResult> Handle(ProvisionTenantMemberCommand request, CancellationToken cancellationToken)
    {
        string operationKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) ||
            !UsersSecurityAuthorization.CanManageTenantMembers(requestContext.User, tenantId))
        {
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return Failure(UsersCommandErrorCodes.Unauthorized);
        }

        string roleName = TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantUser);
        IdentityRole? role = await dbContext.Roles.SingleOrDefaultAsync(item => item.Name == roleName, cancellationToken);
        if (role is null)
        {
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.TenantRoleNotFound, cancellationToken);
            return Failure(UsersCommandErrorCodes.RoleNotFound);
        }

        try
        {
            await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : null;

            User? user = await FindUserAsync(request, cancellationToken);
            bool created = false;
            if (user is null && !string.IsNullOrWhiteSpace(request.UserId))
            {
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
                return Failure(UsersCommandErrorCodes.TenantMemberNotFound);
            }

            if (user is null)
            {
                user = await CreateUserAsync(request, cancellationToken);
                created = true;
            }

            bool hasOwnerRole = await HasRoleAsync(user.Id, TenantRoleDefinitions.BuildRoleName(tenantId, IdentityDefinitions.TenantOwner), cancellationToken);
            bool hasUserRole = await dbContext.UserRoles.AnyAsync(
                membership => membership.UserId == user.Id && membership.RoleId == role.Id,
                cancellationToken);
            if (hasOwnerRole || hasUserRole)
            {
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Succeeded, null, cancellationToken);
                return new ProvisionTenantMemberResult(user, hasOwnerRole ? IdentityDefinitions.TenantOwner : IdentityDefinitions.TenantUser, created, true);
            }

            if (!created && await activeSessionQuery.HasActiveSessionAsync(user.Id, cancellationToken))
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(CancellationToken.None);
                await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ActiveSessionConflict, cancellationToken);
                return Failure(UsersCommandErrorCodes.ActiveSessionConflict);
            }

            dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
            user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Succeeded, null, cancellationToken);
            return new ProvisionTenantMemberResult(user, IdentityDefinitions.TenantUser, created, false);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.TenantRoleConcurrencyConflict, cancellationToken);
            return Failure(UsersCommandErrorCodes.TenantMemberConflict);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            dbContext.ChangeTracker.Clear();
            await WriteAuditAsync(request, tenantId, operationKey, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.TenantRolePersistenceFailure, CancellationToken.None);
            throw;
        }
    }

    private async Task<User?> FindUserAsync(ProvisionTenantMemberCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.UserId))
            return await dbContext.Users.SingleOrDefaultAsync(user => user.Id == request.UserId.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            string normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
            return await dbContext.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        string phone = request.PhoneNumber!.Trim();
        return await dbContext.Users.SingleOrDefaultAsync(user => user.PhoneNumber == phone, cancellationToken);
    }

    private async Task<User> CreateUserAsync(ProvisionTenantMemberCommand request, CancellationToken cancellationToken)
    {
        string contact = request.Email?.Trim() ?? request.PhoneNumber!.Trim();
        User user = new()
        {
            UserName = contact,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim()
        };

        if (request.Email is not null)
            user.Email = request.Email.Trim();
        else
            user.PhoneNumber = contact;

        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        return user;
    }

    private Task<bool> HasRoleAsync(string userId, string roleName, CancellationToken cancellationToken) =>
        (from membership in dbContext.UserRoles
         join role in dbContext.Roles on membership.RoleId equals role.Id
         where membership.UserId == userId && role.Name == roleName
         select membership).AnyAsync(cancellationToken);

    private async Task WriteAuditAsync(ProvisionTenantMemberCommand request, Guid tenantId, string operationKey, string outcome, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = UsersSecurityAuditEvents.TenantMemberProvisioning,
                EventCategory = SecurityAuditEventCatalog.ManagementCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = request.UserId,
                TenantId = tenantId == Guid.Empty ? requestContext.TenantId ?? UsersSecurityAuditEvents.GlobalScope : tenantId.ToString(),
                Resource = UsersSecurityAuditEvents.UserResourcePrefix + (request.UserId ?? "contact"),
                Operation = UsersSecurityAuditEvents.TenantMemberProvisioningOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? operationKey,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = operationKey + UsersSecurityAuditEvents.TenantMemberProvisioningIdempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
        }
    }

    private static ProvisionTenantMemberResult Failure(string errorCode) => new(null, IdentityDefinitions.TenantUser, false, false, errorCode);
}
