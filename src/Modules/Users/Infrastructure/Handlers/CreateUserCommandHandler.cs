using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class CreateUserCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit)
    : IRequestHandler<CreateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (request.User is null || !UserContactIdentifierPolicy.IsValid(request.User.Email, request.User.PhoneNumber, request.User.FirstName, request.User.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(request.User?.Id, cancellationToken);
            return IdentityResult.Failed(new IdentityError { Code = nameof(UsersCommandFailureCode.InvalidContact) });
        }

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(request.User);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, CancellationToken.None);
            throw;
        }

        if (!result.Succeeded)
        {
            await managementAudit.AccountCreationFailedAsync(request.User.Id, cancellationToken);
            return result;
        }

        await managementAudit.AccountCreatedAsync(request.User.Id, CancellationToken.None);
        await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);

        return result;
    }
}
