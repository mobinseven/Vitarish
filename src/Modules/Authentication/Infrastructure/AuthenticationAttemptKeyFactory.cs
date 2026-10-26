using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Infrastructure.Options;

namespace Vitarish.Authentication.Infrastructure;

/// <summary>
/// Creates opaque, realm-scoped authentication attempt keys. The realm is
/// resolved from trusted host configuration, never from request input.
/// </summary>
public sealed class AuthenticationAttemptKeyFactory : IAuthenticationAttemptKeyFactory
{
    private const string SourceKind = "source";
    private const string SourceMethod = "any";

    private readonly KeyMaterial current;
    private readonly KeyMaterial? previous;
    private readonly string realm;

    public AuthenticationAttemptKeyFactory(IOptions<AuthenticationAttemptKeyOptions> options)
        : this(options, null, null)
    {
    }

    public AuthenticationAttemptKeyFactory(
        IOptions<AuthenticationAttemptKeyOptions> options,
        IConfiguration? configuration,
        IHostEnvironment? environment)
    {
        ResolvedAuthenticationAttemptKeyOptions resolved = AuthenticationAttemptKeyOptionsValidator.Resolve(options.Value, configuration, environment);
        realm = resolved.Realm;
        current = new KeyMaterial(resolved.CurrentVersion, Encoding.UTF8.GetBytes(resolved.CurrentPepper));
        previous = string.IsNullOrWhiteSpace(resolved.PreviousPepper)
            ? null
            : new KeyMaterial(resolved.PreviousVersion!, Encoding.UTF8.GetBytes(resolved.PreviousPepper));
    }

    public string Identifier(AuthenticationMethod method, string identifier) => Create(current, "identifier", method.ToString(), identifier);

    public string Subject(AuthenticationMethod method, string userId) => Create(current, "subject", method.ToString(), userId);

    public string Challenge(AuthenticationMethod method, string challengeId) => Create(current, "challenge", method.ToString(), challengeId);

    public string Challenge(AuthenticationChallengePurpose purpose, string userId) =>
        Create(current, "subject", AuthenticationAttemptKeyMethods.MethodFor(purpose).ToString(), userId);

    // Sources are not tied to one authentication method, so every method shares one key per address.
    public string Source(string clientIpAddress) => Create(current, SourceKind, SourceMethod, clientIpAddress);

    public IReadOnlyList<string> IdentifierCandidates(AuthenticationMethod method, string identifier) =>
        CreateCandidates("identifier", method.ToString(), identifier);

    public IReadOnlyList<string> SubjectCandidates(AuthenticationMethod method, string userId) =>
        CreateCandidates("subject", method.ToString(), userId);

    public IReadOnlyList<string> ChallengeCandidates(AuthenticationMethod method, string challengeId) =>
        CreateCandidates("challenge", method.ToString(), challengeId);

    public IReadOnlyList<string> ChallengeCandidates(AuthenticationChallengePurpose purpose, string userId) =>
        CreateCandidates("subject", AuthenticationAttemptKeyMethods.MethodFor(purpose).ToString(), userId);

    public IReadOnlyList<string> SourceCandidates(string clientIpAddress) =>
        CreateCandidates(SourceKind, SourceMethod, clientIpAddress);

    private IReadOnlyList<string> CreateCandidates(string kind, string method, string value)
    {
        string currentKey = Create(current, kind, method, value);
        if (previous is null)
            return [currentKey];

        return [currentKey, Create(previous, kind, method, value)];
    }

    private string Create(KeyMaterial material, string kind, string method, string value)
    {
        string canonical = string.Join('\u001f', material.Version, Realm(), kind, method, Normalize(value));
        byte[] digest = HMACSHA256.HashData(material.Pepper, Encoding.UTF8.GetBytes(canonical));
        return $"{material.Version}:{Base64Url(digest)}";
    }

    private static string Normalize(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private string Realm() => realm;

    private static string Base64Url(ReadOnlySpan<byte> value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record KeyMaterial(string Version, byte[] Pepper);
}
