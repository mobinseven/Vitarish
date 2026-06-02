using Vitarish.Social.Domain.Dto;

namespace Vitarish.Social.Application.Abstractions;

public interface ITokenVault
{
    Task StoreTokenAsync(Guid connectionId, SocialTokenPayload token, CancellationToken cancellationToken);
    Task<SocialTokenPayload?> RetrieveTokenAsync(Guid connectionId, CancellationToken cancellationToken);
    Task RevokeTokenAsync(Guid connectionId, CancellationToken cancellationToken);
}
