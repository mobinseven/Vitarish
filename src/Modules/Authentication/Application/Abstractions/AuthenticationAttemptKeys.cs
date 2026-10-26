using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationAttemptKeyFactory
{
    string Identifier(AuthenticationMethod method, string identifier);
    string Subject(AuthenticationMethod method, string userId);
    string Challenge(AuthenticationMethod method, string challengeId);
    string Challenge(AuthenticationChallengePurpose purpose, string userId);
    /// <summary>The key for every failed attempt from one host-normalized client IP address.</summary>
    string Source(string clientIpAddress);

    IReadOnlyList<string> IdentifierCandidates(AuthenticationMethod method, string identifier) => [Identifier(method, identifier)];
    IReadOnlyList<string> SubjectCandidates(AuthenticationMethod method, string userId) => [Subject(method, userId)];
    IReadOnlyList<string> ChallengeCandidates(AuthenticationMethod method, string challengeId) => [Challenge(method, challengeId)];
    IReadOnlyList<string> ChallengeCandidates(AuthenticationChallengePurpose purpose, string userId) => [Challenge(purpose, userId)];
    IReadOnlyList<string> SourceCandidates(string clientIpAddress) => [Source(clientIpAddress)];
}

public static class AuthenticationAttemptKeyMethods
{
    public static AuthenticationMethod MethodFor(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.EmailLogin => AuthenticationMethod.Email,
        AuthenticationChallengePurpose.PhoneLogin or AuthenticationChallengePurpose.PasswordLoginOtp => AuthenticationMethod.Phone,
        AuthenticationChallengePurpose.EmailPasswordRecovery => AuthenticationMethod.Email,
        AuthenticationChallengePurpose.PhonePasswordRecovery => AuthenticationMethod.Phone,
        _ => AuthenticationMethod.Passkey
    };
}
