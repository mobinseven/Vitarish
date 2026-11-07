using System.Security.Cryptography;
using Vitarish.Authentication.Application.Results;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class PendingAuthenticationChallengeCookie(
    IHttpContextAccessor accessor, IDataProtectionProvider protection) : IPendingAuthenticationChallenge
{
    private const string CookieName = "Authentication.PendingChallenge";
    private const string ProtectionPurpose = "Authentication.PendingChallenge.v1";
    private readonly IDataProtector protector = protection.CreateProtector(ProtectionPurpose);

    public void Store(ChallengeResult challenge, string? returnUrl)
    {
        string value = JsonSerializer.Serialize(new PendingChallengeReference(challenge, returnUrl), PendingChallengeJsonContext.Default.PendingChallengeReference);
        accessor.HttpContext!.Response.Cookies.Append(CookieName, protector.Protect(value), new CookieOptions
        {
            HttpOnly = true, Secure = accessor.HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax, Path = "/", Expires = challenge.ExpiresAt
        });
    }

    public PendingChallengeReference? Read()
    {
        string? value = accessor.HttpContext?.Request.Cookies[CookieName];
        if (value is null) return null;
        try
        {
            PendingChallengeReference? pending = JsonSerializer.Deserialize(protector.Unprotect(value), PendingChallengeJsonContext.Default.PendingChallengeReference);
            return pending;
        }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }

    public void Clear() => accessor.HttpContext!.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
}


[JsonSerializable(typeof(PendingChallengeReference))]
internal partial class PendingChallengeJsonContext : JsonSerializerContext;
