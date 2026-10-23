using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

/// <summary>
/// Changes one namespaced profile entry through this handler's own context, so every attempt reads the
/// user afresh. The user's concurrency stamp guards the write: a change saved meanwhile by any other
/// writer is reported as a conflict instead of being overwritten, and so is an entry that no longer
/// holds the value its owner expected.
/// </summary>
public sealed class SetUserProfileValueCommandHandler(UsersDbContext dbContext, IDistributedCache cache)
    : IRequestHandler<SetUserProfileValueCommand, SetUserProfileValueOutcome>
{
    public async Task<SetUserProfileValueOutcome> Handle(SetUserProfileValueCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)
            || !SetUserProfileValueCommand.IsValidKey(request.Key)
            || request.Value is { Length: > SetUserProfileValueCommand.MaxValueLength })
            return SetUserProfileValueOutcome.Invalid;

        User? user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is null)
            return SetUserProfileValueOutcome.UserNotFound;

        Dictionary<string, string> profile = user.ProfilePreferences;
        string? current = profile.TryGetValue(request.Key, out string? stored) ? stored : null;
        if (request.Expected is { } expected && !string.Equals(current, expected.Value, StringComparison.Ordinal))
            return SetUserProfileValueOutcome.Conflict;
        if (string.Equals(current, request.Value, StringComparison.Ordinal))
            return SetUserProfileValueOutcome.Saved;

        if (request.Value is null)
            _ = profile.Remove(request.Key);
        else
            profile[request.Key] = request.Value;
        user.ProfilePreferences = profile;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return SetUserProfileValueOutcome.Conflict;
        }

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        return SetUserProfileValueOutcome.Saved;
    }
}
