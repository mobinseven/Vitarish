using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Contracts;
using Vitarish.Users.Application.TemporaryAutoAuthLinks;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class TemporaryAutoAuthLinkIssuer(
    UsersDbContext dbContext,
    IOptions<TemporaryAutoAuthLinkOptions> options,
    TimeProvider? timeProvider = null) : ITemporaryAutoAuthLinkIssuer
{
    public async Task<IssueTemporaryAutoAuthLinkResult> IssueAsync(
        IssueTemporaryAutoAuthLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Purpose);
        TemporaryAutoAuthLinkSecurity.ThrowIfUnknownPurpose(request.Purpose);

        TemporaryAutoAuthLinkPurposeOptions purposeOptions = TemporaryAutoAuthLinkSecurity.GetPurposeOptions(options.Value, request.Purpose);
        if (!TemporaryAutoAuthLinkSecurity.IsDestinationAllowed(request.DestinationPath, purposeOptions))
        {
            throw new InvalidOperationException("DestinationPath is not allowed for the requested purpose.");
        }

        bool userExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException("User does not exist.");
        }

        string rawCode = TemporaryAutoAuthLinkSecurity.GenerateRawCode();
        string codeHash = TemporaryAutoAuthLinkSecurity.ComputeCodeHash(rawCode, options.Value);
        DateTimeOffset createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow();
        DateTimeOffset expiresAt = createdAt.AddMinutes(purposeOptions.LifetimeMinutes);

        TemporaryAutoAuthLink entity = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            codeHash,
            request.UserId,
            request.DestinationPath,
            request.Purpose,
            createdAt,
            expiresAt,
            request.CreatedByDeviceHint);

        _ = dbContext.TemporaryAutoAuthLinks.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new IssueTemporaryAutoAuthLinkResult(
            entity.Id,
            rawCode,
            request.Purpose,
            request.DestinationPath,
            createdAt,
            expiresAt);
    }
}

internal static class TemporaryAutoAuthLinkSecurity
{
    private const int RawCodeByteLength = 32;

    public static void ThrowIfUnknownPurpose(string purpose)
    {
        if (!string.Equals(purpose, TemporaryAutoAuthLinkPurposes.AdvancedInputVoice, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unknown temporary auto-auth purpose.");
        }
    }

    public static string GenerateRawCode()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(RawCodeByteLength);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static string ComputeCodeHash(string rawCode, TemporaryAutoAuthLinkOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.HmacSecret);

        byte[] key = Encoding.UTF8.GetBytes(options.HmacSecret);
        byte[] payload = Encoding.UTF8.GetBytes(rawCode);
        using HMACSHA256 hmac = new(key);
        byte[] hash = hmac.ComputeHash(payload);
        return Convert.ToHexString(hash);
    }

    public static TemporaryAutoAuthLinkPurposeOptions GetPurposeOptions(TemporaryAutoAuthLinkOptions options, string purpose) => options.Purposes.TryGetValue(purpose, out TemporaryAutoAuthLinkPurposeOptions? configured)
            ? configured
            : new TemporaryAutoAuthLinkPurposeOptions
            {
                LifetimeMinutes = options.DefaultLifetimeMinutes,
                AllowedDestinationPathPrefixes = ["/"]
            };

    public static bool IsDestinationAllowed(string destinationPath, TemporaryAutoAuthLinkPurposeOptions options) => IsLocalUrl(destinationPath) && options.AllowedDestinationPathPrefixes.Length != 0 && options.AllowedDestinationPathPrefixes.Any(prefix =>
                                                                                                                             !string.IsNullOrWhiteSpace(prefix)
                                                                                                                             && (string.Equals(prefix, "/", StringComparison.Ordinal)
                                                                                                                                 || destinationPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

    private static bool IsLocalUrl(string? url) => !string.IsNullOrWhiteSpace(url) && (url[0] == '/' ? url.Length <= 1 || url[1] != '/' : url.Length > 1 && url[0] == '~' && url[1] == '/');
}
