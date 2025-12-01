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

internal sealed class DeleteUserHandler(
    UsersDbContext db,
    IPublisher publisher,
    IDistributedCache cache,
    ILogger<DeleteUserHandler> logger) : IRequestHandler<DeleteUser, Result>
{
    public async Task<Result> Handle(DeleteUser request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("User with ID '{UserId}' not found for deletion", request.Id);
            return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{request.Id}' was not found."));
        }

        _ = db.Users.Remove(user);

        try
        {
            _ = await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to delete user '{UserId}'", request.Id);
            return Result.Failure(Error.Failure("User.DeleteFailed", $"Failed to delete user '{request.Id}'."));
        }

        await publisher.Publish(new UserDeletedEvent(user.Id), cancellationToken);
        await UserCacheExtensions.RemoveUserAsync(cache, user, cancellationToken);

        return Result.Success();
    }
}
