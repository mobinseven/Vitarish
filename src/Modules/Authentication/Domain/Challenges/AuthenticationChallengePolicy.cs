namespace Vitarish.Authentication.Domain.Challenges;

public sealed record AuthenticationChallengePolicy(TimeSpan Lifetime, int MaximumAttempts)
{
    public static AuthenticationChallengePolicy Default { get; } = new(TimeSpan.FromMinutes(5), 5);

    private static readonly string PhoneLoginTokenPurpose = "Authentication:PhoneLogin";
    private static readonly string EmailLoginTokenPurpose = "Authentication:EmailLogin";
    private static readonly string PasskeyTokenPurpose = "Authentication:PasskeyLogin";

    public bool IsExpired(DateTimeOffset createdAt, DateTimeOffset now) => now >= createdAt.Add(Lifetime);
    public bool HasAttemptsRemaining(int failedAttempts) => failedAttempts < MaximumAttempts;

    public string TokenPurpose(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.PhoneLogin => PhoneLoginTokenPurpose,
        AuthenticationChallengePurpose.EmailLogin => EmailLoginTokenPurpose,
        AuthenticationChallengePurpose.PasskeyLogin => PasskeyTokenPurpose,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    public AuthenticationChallenge CreatePhoneLoginChallenge(string userId, string phoneNumber, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhoneLogin, userId, phoneNumber, tokenPurpose, now);

    public AuthenticationChallenge CreateEmailLoginChallenge(string userId, string email, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailLogin, userId, email, tokenPurpose, now);

    public AuthenticationChallenge CreatePasskeyLoginChallenge(string userIdOrLoginHint, string challengeReference, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PasskeyLogin, userIdOrLoginHint, challengeReference, PasskeyTokenPurpose, now);

    private AuthenticationChallenge CreateChallenge(AuthenticationChallengePurpose purpose, string userId, string destination, string tokenPurpose, DateTimeOffset now) =>
        new(
            new AuthenticationChallengeId(Guid.NewGuid().ToString("N")),
            purpose,
            userId,
            destination,
            tokenPurpose,
            now,
            now.Add(Lifetime),
            MaximumAttempts);
}
