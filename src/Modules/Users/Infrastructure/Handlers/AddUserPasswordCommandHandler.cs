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

public class AddUserPasswordCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<AddUserPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(AddUserPasswordCommand request, CancellationToken cancellationToken)
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
            await managementAudit.SecurityAttributeChangedAsync(request.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (user is null)
        {
            await managementAudit.AccountEditFailedAsync(request.Id, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(request.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.UserNotFound, cancellationToken);
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.UserNotFound,
                Description = UsersCommandErrorCodes.UserNotFoundDescription
            });
        }

        IdentityResult result;
        try
        {
            result = await userManager.AddPasswordAsync(user, request.Password);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Failed, SecurityAuditReasonCatalog.MutationRejected, CancellationToken.None);
            throw;
        }

        if (result.Succeeded)
        {
            await managementAudit.AccountEditedAsync(user.Id, SecurityAuditReasonCatalog.SecurityAttributeChanged, CancellationToken.None);
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }
        else
        {
            await managementAudit.AccountEditFailedAsync(user.Id, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            await managementAudit.SecurityAttributeChangedAsync(user.Id, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        }

        return result;
    }
}
