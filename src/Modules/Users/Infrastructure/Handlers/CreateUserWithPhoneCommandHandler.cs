using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Security;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CreateUserWithPhoneCommandHandler(UserManager<User> userManager, ISender sender, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<CreateUserWithPhoneCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        if (!UserPhoneNumberPolicy.TryNormalize(request.PhoneNumber, out string? phoneNumber) ||
            !UserContactIdentifierPolicy.IsValid(null, phoneNumber, request.FirstName, request.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
            return null;
        }

        User? user;
        try
        {
            user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
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
            bool phoneOwnedByAnotherAccount = await sender.Send(
                new CheckLoginIdentifierInUseQuery(phoneNumber, LoginIdentifierKind.Phone, null), cancellationToken);
            if (phoneOwnedByAnotherAccount)
            {
                user = await userManager.Users.SingleOrDefaultAsync(
                    candidate => candidate.NormalizedUserName == phoneNumber, cancellationToken);
                if (user is not null && request.RejectIfAlreadyExists)
                {
                    await managementAudit.AccountCreationFailedAsync(user.Id, cancellationToken);
                    return null;
                }
            }
        }

        if (user is null)
        {
            try
            {
                user = await userManager.CreateUserWithoutPasswordAsync(
                    phoneNumber,
                    request.FirstName,
                    request.LastName,
                    u => userManager.SetPhoneNumberAsync(u, phoneNumber),
                    request.IdentificationCode,
                    request.DateOfBirth,
                    request.BirthCountryCode,
                    request.BirthCity);
            }
            catch (CreateUserFailureException exception) when (IsIdentifierConflict(exception))
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                User? racingOwner = await userManager.Users.FirstOrDefaultAsync(
                    candidate => candidate.PhoneNumber == phoneNumber || candidate.NormalizedUserName == phoneNumber, cancellationToken);
                if (racingOwner is not null)
                    return request.RejectIfAlreadyExists ? null : racingOwner;
                throw;
            }
            catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                User? racingOwner = await userManager.Users.FirstOrDefaultAsync(
                    candidate => candidate.PhoneNumber == phoneNumber || candidate.NormalizedUserName == phoneNumber, cancellationToken);
                if (racingOwner is not null)
                    return request.RejectIfAlreadyExists ? null : racingOwner;
                throw;
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

    private static bool IsIdentifierConflict(CreateUserFailureException exception) =>
        exception.Result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail");
}
