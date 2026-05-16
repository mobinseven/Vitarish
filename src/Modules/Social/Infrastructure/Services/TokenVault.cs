using Microsoft.AspNetCore.DataProtection;

namespace Vitarish.Social.Infrastructure.Services;

[RegisterTransient]
internal sealed class TokenVault(
    IDataProtectionProvider protectionProvider,
    IEntityRepository<SocialConnectionToken, SocialConnectionTokenDescriptor> tokenRepository,
    IEntityRepository<SocialConnection, SocialConnectionDescriptor> connectionRepository) : ITokenVault
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("Vitarish.Social.TokenVault.v1");

    public async Task StoreTokenAsync(Guid connectionId, SocialTokenPayload token, CancellationToken cancellationToken)
    {
        SocialConnection? connection = await connectionRepository.GetAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            throw new InvalidOperationException($"Social connection '{connectionId}' was not found.");
        }

        SocialConnectionTokenDescriptor descriptor = new() { ConnectionId = connectionId, Limit = 1 };
        SocialConnectionToken? existing = (await tokenRepository.GetAsync(descriptor, cancellationToken)).FirstOrDefault();

        string encryptedAccess = _protector.Protect(token.AccessToken);
        string? encryptedRefresh = token.RefreshToken is null ? null : _protector.Protect(token.RefreshToken);
        string scopes = string.Join(' ', token.Scopes);

        if (existing is null)
        {
            SocialConnectionToken entity = new()
            {
                ConnectionId = connectionId,
                AccessToken = encryptedAccess,
                RefreshToken = encryptedRefresh,
                ExpiresAt = token.ExpiresAt,
                Scopes = scopes,
                LastRefreshedAt = DateTimeOffset.UtcNow,
            };

            _ = await tokenRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.AccessToken = encryptedAccess;
            existing.RefreshToken = encryptedRefresh;
            existing.ExpiresAt = token.ExpiresAt;
            existing.Scopes = scopes;
            existing.LastRefreshedAt = DateTimeOffset.UtcNow;

            _ = await tokenRepository.UpdateAsync(existing, cancellationToken);
        }

        connection.Status = token.ExpiresAt is { } expiresAt && expiresAt <= DateTimeOffset.UtcNow
            ? SocialConnectionStatus.TokenExpired
            : SocialConnectionStatus.Active;

        _ = await connectionRepository.UpdateAsync(connection, cancellationToken);
    }

    public async Task<SocialTokenPayload?> RetrieveTokenAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        SocialConnectionTokenDescriptor descriptor = new() { ConnectionId = connectionId, Limit = 1 };
        SocialConnectionToken? token = (await tokenRepository.GetAsync(descriptor, cancellationToken)).FirstOrDefault();
        if (token is null)
        {
            return null;
        }

        string access = _protector.Unprotect(token.AccessToken);
        string? refresh = token.RefreshToken is null ? null : _protector.Unprotect(token.RefreshToken);
        IReadOnlyList<string> scopes = string.IsNullOrWhiteSpace(token.Scopes)
            ? []
            : token.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new SocialTokenPayload(access, refresh, token.ExpiresAt, scopes);
    }

    public async Task RevokeTokenAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        SocialConnectionTokenDescriptor descriptor = new() { ConnectionId = connectionId };
        IReadOnlyList<SocialConnectionToken> tokens = await tokenRepository.GetAsync(descriptor, cancellationToken);

        if (tokens.Count > 0)
        {
            await tokenRepository.RemoveRangeAsync(tokens, cancellationToken);
        }

        SocialConnection? connection = await connectionRepository.GetAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            return;
        }

        connection.Status = SocialConnectionStatus.ReconnectRequired;
        _ = await connectionRepository.UpdateAsync(connection, cancellationToken);
    }
}
