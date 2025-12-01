using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Core.Users.Domain.Events;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Extensions;

namespace Vitarish.Identity.Infrastructure.Users.Handlers;

internal sealed class UpdateUserHandler(
    UsersDbContext db,
    IPublisher publisher,
    IDistributedCache cache,
    ILogger<UpdateUserHandler> logger) : IRequestHandler<UpdateUser, Result>
{
    public async Task<Result> Handle(UpdateUser request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("User with ID '{UserId}' not found for update", request.Id);
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{request.Id}' was not found."));
        }

        user.UpdateName(request.FirstName, request.LastName);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict updating user '{UserId}'", request.Id);
            return Result.Failure(Error.Conflict("User.ConcurrencyConflict", "The user was modified by another process."));
        }

        await publisher.Publish(new UserUpdatedEvent(user.Id), cancellationToken);
        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);

        return Result.Success();
    }
}
