using Vitarish.Users.Application.Contracts;

namespace Vitarish.Users.Application.Abstractions;

public interface ITemporaryAutoAuthLinkRedeemer
{
    Task<RedeemTemporaryAutoAuthLinkResult> RedeemAsync(
        RedeemTemporaryAutoAuthLinkRequest request,
        CancellationToken cancellationToken = default);
}
