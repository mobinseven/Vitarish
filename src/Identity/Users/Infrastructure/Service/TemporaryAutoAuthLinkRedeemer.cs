using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Contracts;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class TemporaryAutoAuthLinkRedeemer(
    UsersDbContext dbContext,
    UserManager<User> userManager,
    IOptions<TemporaryAutoAuthLinkOptions> options) : ITemporaryAutoAuthLinkRedeemer
{
    public async Task<RedeemTemporaryAutoAuthLinkResult> RedeemAsync(
        RedeemTemporaryAutoAuthLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawCode) || string.IsNullOrWhiteSpace(request.Purpose))
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        try
        {
            TemporaryAutoAuthLinkSecurity.ThrowIfUnknownPurpose(request.Purpose);
        }
        catch (InvalidOperationException)
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(request.RawCode, options.Value);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        TemporaryAutoAuthLink? link = await dbContext.TemporaryAutoAuthLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                l => l.CodeHash == codeHash
                     && l.Purpose == request.Purpose,
                cancellationToken);
        if (link is null)
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        TemporaryAutoAuthLinkPurposeOptions purposeOptions = TemporaryAutoAuthLinkSecurity.GetPurposeOptions(options.Value, request.Purpose);
        if (!TemporaryAutoAuthLinkSecurity.IsDestinationAllowed(link.DestinationPath, purposeOptions))
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        User? user = await userManager.FindByIdAsync(link.UserId);
        if (user is null)
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        int updated = await dbContext.TemporaryAutoAuthLinks
            .Where(l => l.Id == link.Id
                        && l.CodeHash == codeHash
                        && l.ConsumedAt == null
                        && l.ExpiresAt > now)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(l => l.ConsumedAt, now)
                    .SetProperty(l => l.ConsumedByIp, request.ConsumedByIp)
                    .SetProperty(l => l.ConsumedByUserAgent, request.ConsumedByUserAgent),
                cancellationToken);

        if (updated != 1)
        {
            return new RedeemTemporaryAutoAuthLinkResult(false);
        }

        return new RedeemTemporaryAutoAuthLinkResult(
            true,
            link.Id,
            link.UserId,
            link.DestinationPath);
    }
}
