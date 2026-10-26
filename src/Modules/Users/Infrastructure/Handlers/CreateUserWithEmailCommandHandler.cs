using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CreateUserWithEmailCommandHandler(UserManager<User> userManager, ISender sender, IDistributedCache cache, UsersManagementAudit managementAudit) : IRequestHandler<CreateUserWithEmailCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithEmailCommand request, CancellationToken cancellationToken)
    {
        if (!UserContactIdentifierPolicy.IsValid(request.Email, null, request.FirstName, request.LastName))
        {
            await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
            return null;
        }

        string? normalizedEmail = userManager.NormalizeEmail(request.Email);

        User? user;
        try
        {
            user = await userManager.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
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
            bool emailOwnedByAnotherAccount = await sender.Send(
                new CheckLoginIdentifierInUseQuery(request.Email, LoginIdentifierKind.Email, null), cancellationToken);
            if (emailOwnedByAnotherAccount)
            {
                user = await userManager.Users.SingleOrDefaultAsync(
                    candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);
                if (user is null)
                {
                    await managementAudit.AccountCreationFailedAsync(null, cancellationToken);
                    return null;
                }

                if (request.RejectIfAlreadyExists)
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
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    u => userManager.SetEmailAsync(u, request.Email),
                    request.IdentificationCode,
                    request.DateOfBirth,
                    request.BirthCountryCode,
                    request.BirthCity);
            }
            catch (CreateUserFailureException exception) when (IsIdentifierConflict(exception))
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                User? racingOwner = await FindRacingOwnerAsync(request.Email, cancellationToken);
                if (racingOwner is not null)
                    return request.RejectIfAlreadyExists ? null : racingOwner;
                return null;
            }
            catch (DbUpdateException exception) when (ContactIdentifierUniqueViolation.Matches(exception))
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                User? racingOwner = await FindRacingOwnerAsync(request.Email, cancellationToken);
                if (racingOwner is not null)
                    return request.RejectIfAlreadyExists ? null : racingOwner;
                return null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await managementAudit.AccountCreationFailedAsync(null, CancellationToken.None);
                throw;
            }

            await managementAudit.AccountCreatedAsync(
                user.Id,
                UserAuditChanges.Between((null, null, null, null), (user.Email, user.PhoneNumber, user.FirstName, user.LastName)),
                CancellationToken.None);
        }

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }

    private async Task<User?> FindRacingOwnerAsync(string email, CancellationToken cancellationToken)
    {
        string? normalizedEmail = userManager.NormalizeEmail(email);
        return await userManager.Users.FirstOrDefaultAsync(
            candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private static bool IsIdentifierConflict(CreateUserFailureException exception) =>
        exception.Result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail");
}
