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
    IOptions<TemporaryAutoAuthLinkOptions> options,
    TimeProvider? timeProvider = null) : ITemporaryAutoAuthLinkRedeemer
{
    private static readonly SemaphoreSlim FallbackLock = new(1, 1);

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
        DateTimeOffset now = (timeProvider ?? TimeProvider.System).GetUtcNow();

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

        int updated = dbContext.Database.IsRelational()
            ? await dbContext.TemporaryAutoAuthLinks
                .Where(l => l.Id == link.Id
                            && l.CodeHash == codeHash
                            && l.ConsumedAt == null
                            && l.ExpiresAt > now)
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(l => l.ConsumedAt, now)
                        .SetProperty(l => l.ConsumedByIp, request.ConsumedByIp)
                        .SetProperty(l => l.ConsumedByUserAgent, request.ConsumedByUserAgent),
                    cancellationToken)
            : await ConsumeWithInMemoryFallbackAsync(link.Id, codeHash, now, request.ConsumedByIp, request.ConsumedByUserAgent, cancellationToken);

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

    private async Task<int> ConsumeWithInMemoryFallbackAsync(
        Guid linkId,
        string codeHash,
        DateTimeOffset now,
        string? consumedByIp,
        string? consumedByUserAgent,
        CancellationToken cancellationToken)
    {
        await FallbackLock.WaitAsync(cancellationToken);
        try
        {
            TemporaryAutoAuthLink? tracked = await dbContext.TemporaryAutoAuthLinks
                .SingleOrDefaultAsync(
                    l => l.Id == linkId
                         && l.CodeHash == codeHash
                         && l.ConsumedAt == null
                         && l.ExpiresAt > now,
                    cancellationToken);
            if (tracked is null)
            {
                return 0;
            }

            tracked.MarkConsumed(now, consumedByIp, consumedByUserAgent);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return 1;
        }
        finally
        {
            _ = FallbackLock.Release();
        }
    }
}
