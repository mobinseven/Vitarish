using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewUserProfileService : IUserProfileService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Dictionary<string, string>> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<Dictionary<string, string>> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        if (_profiles.TryGetValue(userId, out Dictionary<string, string>? preferences))
        {
            return Task.FromResult(new Dictionary<string, string>(preferences));
        }

        return Task.FromResult(new Dictionary<string, string>());
    }

    public Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences, CancellationToken ct = default)
    {
        _profiles.AddOrUpdate(
            userId,
            _ => new Dictionary<string, string>(preferences),
            (_, _) => new Dictionary<string, string>(preferences));
        return Task.CompletedTask;
    }
}
