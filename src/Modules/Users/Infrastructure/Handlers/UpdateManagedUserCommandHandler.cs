using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class UpdateManagedUserCommandHandler(
    UserManager<User> userManager,
    IDistributedCache cache,
    UsersManagementAudit managementAudit,
    IRequestContext requestContext) : IRequestHandler<UpdateManagedUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateManagedUserCommand request, CancellationToken cancellationToken)
    {
        if (!UsersSecurityAuthorization.CanManageAccounts(requestContext.User))
        {
            await managementAudit.AccountEditFailedAsync(request.UserId, SecurityAuditReasonCatalog.Unauthorized, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Code = nameof(UsersCommandFailureCode.Unauthorized) });
        }

        string? email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        string? phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        string? firstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
        string? lastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();

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
            await managementAudit.AccountEditFailedAsync(request.UserId, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(request.UserId, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.UserNotFound),
            });
        }

        bool contactIdentifierChanged =
            !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal);

        if (!UserContactIdentifierPolicy.IsValid(email, phoneNumber, firstName, lastName))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (contactIdentifierChanged)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(UsersCommandFailureCode.InvalidContact),
                Description = UsersCommandErrorCodes.InvalidContactDescription
            });
        }

        user.Email = email;
        user.PhoneNumber = phoneNumber;
        user.FirstName = firstName;
        user.LastName = lastName;

        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            if (contactIdentifierChanged)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (!result.Succeeded)
        {
            string reason = result.Errors.Any(error => string.Equals(error.Code, nameof(UsersCommandFailureCode.ConcurrencyFailure), StringComparison.Ordinal))
                ? SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict
                : SecurityAuditReasonCatalog.MutationRejected;
            await managementAudit.AccountEditFailedAsync(user.Id, reason, cancellationToken);
            if (contactIdentifierChanged)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, reason, cancellationToken);
            return result;
        }

        await managementAudit.AccountEditedAsync(
            user.Id,
            contactIdentifierChanged ? UsersSecurityAuditEvents.UserContactIdentifierChangedReason : null,
            CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return result;
    }
}
