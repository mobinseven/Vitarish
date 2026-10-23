using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

/// <summary>
/// Changes one namespaced profile entry. The user's concurrency stamp guards the write, so a change
/// made meanwhile by another writer is reported as a conflict instead of being overwritten.
/// </summary>
public sealed class SetUserProfileValueCommandHandler(UserManager<User> userManager, IDistributedCache cache)
    : IRequestHandler<SetUserProfileValueCommand, SetUserProfileValueOutcome>
{
    public async Task<SetUserProfileValueOutcome> Handle(SetUserProfileValueCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)
            || !SetUserProfileValueCommand.IsValidKey(request.Key)
            || request.Value is { Length: > SetUserProfileValueCommand.MaxValueLength })
            return SetUserProfileValueOutcome.Invalid;

        User? user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return SetUserProfileValueOutcome.UserNotFound;

        Dictionary<string, string> profile = user.ProfilePreferences;
        if (request.Value is null)
        {
            if (!profile.Remove(request.Key))
                return SetUserProfileValueOutcome.Saved;
        }
        else
        {
            profile[request.Key] = request.Value;
        }

        user.ProfilePreferences = profile;
        IdentityResult result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure))
                ? SetUserProfileValueOutcome.Conflict
                : throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        return SetUserProfileValueOutcome.Saved;
    }
}
