using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CreateUserWithEmailCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<CreateUserWithEmailCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithEmailCommand request, CancellationToken cancellationToken)
    {
        if (!UserContactIdentifierPolicy.IsValid(request.Email, null, request.FirstName, request.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
            return null;
        }

        User? user;
        try
        {
            user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
            throw;
        }

        if (user is not null && request.RejectIfAlreadyExists)
        {
            await managementAudit.AccountCreationFailedAsync(user.Id, cancellationToken);
            return null;
        }

        if (user is null)
        {
            try
            {
                user = await userManager.CreateUserWithoutPasswordAsync(
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    u => userManager.SetEmailAsync(u, request.Email),
                    request.IdentificationCode,
                    request.DateOfBirth,
                    request.BirthCountryCode,
                    request.BirthCity);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                throw;
            }

            await managementAudit.AccountCreatedAsync(user.Id, CancellationToken.None);
        }

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }
}
