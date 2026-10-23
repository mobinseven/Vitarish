
using MediatR;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Infrastructure.Service;

/// <inheritdoc />
public class UserProfileService(ISender sender) : IUserProfileService
{
    public Task<Dictionary<string, string>> GetProfileAsync(string userId, CancellationToken ct = default) =>
        sender.Send(new GetUserProfileQuery(userId), ct);

    public Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default) =>
        sender.Send(new UpdateUserProfileCommand(userId, preferences), ct);

    public Task<SetUserProfileValueOutcome> SetValueAsync(string userId, string key, string? value, ProfileValueExpectation? expected = null, CancellationToken ct = default) =>
        sender.Send(new SetUserProfileValueCommand(userId, key, value, expected), ct);
}
