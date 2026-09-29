using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class SetUserPhoneNumberCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        User? user;
        try
        {
            user = await userManager.FindByIdAsync(request.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(request.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(request.Id, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        string? phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (!UserContactIdentifierPolicy.IsValid(user.Email, phoneNumber, user.FirstName, user.LastName))
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.InvalidContact,
                Description = UsersCommandErrorCodes.InvalidContactDescription
            });
        }

        bool changed = !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.Ordinal);
        IdentityResult result;
        try
        {
            result = await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (result.Succeeded)
        {
            await managementAudit.AccountEditedAsync(
                user.Id,
                changed ? SecurityAuditReasonCatalog.UserContactIdentifierChanged : null,
                CancellationToken.None);
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (changed)
                await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        }

        return result;
    }
}
