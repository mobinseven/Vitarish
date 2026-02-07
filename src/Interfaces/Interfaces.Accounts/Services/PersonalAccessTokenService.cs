using System.Collections.Concurrent;

namespace Vitarish.Interfaces.Accounts.Services;

public interface IPersonalAccessTokenService
{
    Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> ListTokensAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(
        string subjectId,
        string token,
        CancellationToken cancellationToken = default);
}

public sealed class PersonalAccessTokenService(IPersonalAccessTokenGenerator generator)
    : IPersonalAccessTokenService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> tokenStore = new();

    public async Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default)
    {
        string token = await generator.CreateTokenAsync(subjectId, lifetimeDays, scopes, isReferenceToken, cancellationToken);
        ConcurrentDictionary<string, byte> tokens = tokenStore.GetOrAdd(subjectId, _ => new ConcurrentDictionary<string, byte>());
        _ = tokens.TryAdd(token, 0);
        return token;
    }

    public Task<IReadOnlyCollection<string>> ListTokensAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return !tokenStore.TryGetValue(subjectId, out ConcurrentDictionary<string, byte>? tokens)
            ? Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>())
            : Task.FromResult<IReadOnlyCollection<string>>(tokens.Keys.ToArray());
    }

    public Task RevokeTokenAsync(
        string subjectId,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (tokenStore.TryGetValue(subjectId, out ConcurrentDictionary<string, byte>? tokens))
        {
            _ = tokens.TryRemove(token, out _);
        }

        return Task.CompletedTask;
    }
}
