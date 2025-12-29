using System.Collections.Concurrent;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Accounts.Infrastructure.Services;

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
    private static readonly ConcurrentDictionary<string, HashSet<string>> TokenStore = new();

    public async Task<string> CreateTokenAsync(
        string subjectId,
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default)
    {
        string token = await generator.CreateTokenAsync(subjectId, lifetimeDays, scopes, isReferenceToken, cancellationToken);
        HashSet<string> tokens = TokenStore.GetOrAdd(subjectId, _ => []);
        lock (tokens)
        {
            tokens.Add(token);
        }
        return token;
    }

    public Task<IReadOnlyCollection<string>> ListTokensAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TokenStore.TryGetValue(subjectId, out HashSet<string>? tokens))
        {
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        lock (tokens)
        {
            return Task.FromResult<IReadOnlyCollection<string>>(tokens.ToArray());
        }
    }

    public Task RevokeTokenAsync(
        string subjectId,
        string token,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TokenStore.TryGetValue(subjectId, out HashSet<string>? tokens))
        {
            lock (tokens)
            {
                _ = tokens.Remove(token);
            }
        }

        return Task.CompletedTask;
    }
}
