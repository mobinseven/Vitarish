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

public class CreateUserWithPhoneCommandHandler(UserManager<User> userManager, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<CreateUserWithPhoneCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        if (!UserContactIdentifierPolicy.IsValid(null, request.PhoneNumber, request.FirstName, request.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
            return null;
        }

        User? user;
        try
        {
            user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
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
                    request.PhoneNumber,
                    request.FirstName,
                    request.LastName,
                    u => userManager.SetPhoneNumberAsync(u, request.PhoneNumber),
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
