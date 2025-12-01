using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Core.Users.Domain.Events;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Extensions;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class CreateUserHandler(
    UsersDbContext db,
    IPublisher publisher,
    IDistributedCache cache,
    ILogger<CreateUserHandler> logger) : IRequestHandler<CreateUserWithEmail, Result<User>>, IRequestHandler<CreateUserWithPhone, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserWithEmail request, CancellationToken cancellationToken)
    {
        User user = new(
            Guid.NewGuid().ToString(),
            request.Email,
            request.Email,
            phoneNumber: null,
            request.FirstName,
            request.LastName);

        return await PersistUserAsync(user, cancellationToken);
    }

    public async Task<Result<User>> Handle(CreateUserWithPhone request, CancellationToken cancellationToken)
    {
        User user = new(
            Guid.NewGuid().ToString(),
            request.PhoneNumber,
            email: null,
            request.PhoneNumber,
            request.FirstName,
            request.LastName);

        return await PersistUserAsync(user, cancellationToken);
    }

    private async Task<Result<User>> PersistUserAsync(User user, CancellationToken cancellationToken)
    {
        Result<User>? duplicateResult = await CheckForDuplicateIdentifiersAsync(user, cancellationToken);
        if (duplicateResult is not null)
        {
            return duplicateResult;
        }

        _ = db.Users.Add(user);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogWarning(ex, "Failed to create user with ID '{UserId}' due to duplicate key constraint", user.Id);
            return Result.Failure<User>(Error.Conflict(
                "User.AlreadyExists",
                $"A user with ID '{user.Id}' already exists."));
        }

        await publisher.Publish(new UserCreatedEvent(user.Id), cancellationToken);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);

        return Result.Success(user);
    }

    private async Task<Result<User>?> CheckForDuplicateIdentifiersAsync(User user, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.UserName == user.UserName, cancellationToken))
        {
            return Result.Failure<User>(Error.Conflict(
                "User.UserNameExists",
                $"A user with username '{user.UserName}' already exists."));
        }

        if (!string.IsNullOrWhiteSpace(user.Email) &&
            await db.Users.AnyAsync(u => u.Email == user.Email, cancellationToken))
        {
            return Result.Failure<User>(Error.Conflict(
                "User.EmailExists",
                $"A user with email '{user.Email}' already exists."));
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber) &&
            await db.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber, cancellationToken))
        {
            return Result.Failure<User>(Error.Conflict(
                "User.PhoneNumberExists",
                $"A user with phone number '{user.PhoneNumber}' already exists."));
        }

        return null;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
