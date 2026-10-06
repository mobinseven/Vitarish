using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class ChangeUserAccountStatusCommandHandler(
    UserManager<User> userManager,
    IAuthenticationActiveSessionQuery activeSessionQuery,
    IRequestContext requestContext,
    UsersManagementAudit managementAudit,
    IDistributedCache cache) : IRequestHandler<ChangeUserAccountStatusCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(ChangeUserAccountStatusCommand request, CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanManageAccounts(requestContext.User))
        {
            await managementAudit.AccountStatusChangedAsync(
                request.UserId,
                request.Enabled,
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.Unauthorized,
                cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.Unauthorized,
                Description = UsersCommandErrorCodes.UnauthorizedDescription
            });
        }

        User? user;
        try
        {
            user = await userManager.FindByIdAsync(request.UserId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountStatusChangedAsync(
                request.UserId,
                request.Enabled,
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.MutationRejected,
                CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountStatusChangedAsync(
                request.UserId,
                request.Enabled,
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.UserNotFound,
                cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.UserNotFound,
                Description = UsersCommandErrorCodes.UserNotFoundDescription
            });
        }

        if (user.IsEnabled == request.Enabled)
        {
            // Idempotent replay: the account already has the requested status.
            await managementAudit.AccountStatusChangedAsync(
                user.Id,
                request.Enabled,
                SecurityAuditOutcome.Succeeded,
                null,
                cancellationToken);
            return IdentityResult.Success;
        }

        if (!request.Enabled && await activeSessionQuery.HasActiveSessionAsync(user.Id, cancellationToken))
        {
            await managementAudit.AccountStatusChangedAsync(
                user.Id,
                request.Enabled,
                SecurityAuditOutcome.Denied,
                SecurityAuditReasonCatalog.ActiveSessionConflict,
                cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.ActiveSessionConflict,
                Description = UsersCommandErrorCodes.ActiveSessionConflictDescription
            });
        }

        user.IsEnabled = request.Enabled;
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await managementAudit.AccountStatusChangedAsync(
                user.Id,
                request.Enabled,
                SecurityAuditOutcome.Failed,
                SecurityAuditReasonCatalog.MutationRejected,
                CancellationToken.None);
            throw;
        }

        if (!result.Succeeded)
        {
            bool concurrencyConflict = result.Errors.Any(error =>
                string.Equals(error.Code, nameof(UsersCommandFailureCode.ConcurrencyFailure), StringComparison.Ordinal));
            await managementAudit.AccountStatusChangedAsync(
                user.Id,
                request.Enabled,
                SecurityAuditOutcome.Denied,
                concurrencyConflict
                    ? SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict
                    : SecurityAuditReasonCatalog.MutationRejected,
                cancellationToken);
            return concurrencyConflict
                ? IdentityResult.Failed(new IdentityError
                {
                    Code = UsersCommandErrorCodes.ConcurrencyFailure,
                    Description = UsersCommandErrorCodes.ConcurrencyFailureDescription
                })
                : result;
        }

        await managementAudit.AccountStatusChangedAsync(
            user.Id,
            request.Enabled,
            SecurityAuditOutcome.Succeeded,
            null,
            CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return result;
    }
}
