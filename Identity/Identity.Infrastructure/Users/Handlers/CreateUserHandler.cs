using MediatR;
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
        _ = db.Users.Add(user);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
                                            ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
                                            ex.InnerException?.Message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) == true)
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
}
