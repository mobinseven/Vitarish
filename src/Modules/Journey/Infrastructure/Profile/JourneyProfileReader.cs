using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Journey.Infrastructure.Profile;

/// <summary>
/// Reads profile/contact completion state for journey evaluation.
/// </summary>
public sealed class JourneyProfileReader(IUsersService usersService) : IJourneyProfileReader
{
    public async Task<JourneyProfileSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        User? user = await usersService.GetUserAsync(userId, cancellationToken);
        if (user is null)
            return null;

        // IdentityUser.EmailConfirmed and PhoneNumberConfirmed are bool? (nullable)
        bool hasConfirmedEmail = user.EmailConfirmed == true;
        bool hasConfirmedPhone = user.PhoneNumberConfirmed == true;

        return new JourneyProfileSnapshot(
            user.Id,
            user.FirstName,
            user.LastName,
            user.FullName,
            null, // PreferredLanguage - not yet in User entity
            null, // TimeZone - not yet in User entity
            hasConfirmedEmail,
            hasConfirmedPhone
        );
    }
}
