namespace Vitarish.Authentication.Domain.Challenges;

public sealed record AuthenticationChallengePolicy(TimeSpan Lifetime, int MaximumAttempts)
{
    public static AuthenticationChallengePolicy Default { get; } = new(TimeSpan.FromMinutes(5), 5);

    private static readonly string PhoneLoginTokenPurpose = "Authentication:PhoneLogin";
    private static readonly string PasswordLoginOtpTokenPurpose = "Authentication:PasswordLoginOtp";
    private static readonly string EmailLoginTokenPurpose = "Authentication:EmailLogin";
    private static readonly string PasskeyTokenPurpose = "Authentication:PasskeyLogin";
    private static readonly string PhoneChangeTokenPurpose = "Authentication:PhoneChange";
    private static readonly string EmailChangeTokenPurpose = "Authentication:EmailChange";
    private static readonly string EmailPasswordRecoveryTokenPurpose = "Authentication:EmailPasswordRecovery";
    private static readonly string PhonePasswordRecoveryTokenPurpose = "Authentication:PhonePasswordRecovery";

    public bool IsExpired(DateTimeOffset createdAt, DateTimeOffset now) => now >= createdAt.Add(Lifetime);
    public bool HasAttemptsRemaining(int failedAttempts) => failedAttempts < MaximumAttempts;

    public AuthenticationChallengePolicy WithMaximumAttempts(int maximumAttempts)
    {
        if (maximumAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Maximum attempts must be positive.");

        return this with { MaximumAttempts = maximumAttempts };
    }

    public string TokenPurpose(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.PhoneLogin => PhoneLoginTokenPurpose,
        AuthenticationChallengePurpose.PasswordLoginOtp => PasswordLoginOtpTokenPurpose,
        AuthenticationChallengePurpose.EmailLogin => EmailLoginTokenPurpose,
        AuthenticationChallengePurpose.PasskeyLogin => PasskeyTokenPurpose,
        AuthenticationChallengePurpose.PhoneChange => PhoneChangeTokenPurpose,
        AuthenticationChallengePurpose.EmailChange => EmailChangeTokenPurpose,
        AuthenticationChallengePurpose.EmailPasswordRecovery => EmailPasswordRecoveryTokenPurpose,
        AuthenticationChallengePurpose.PhonePasswordRecovery => PhonePasswordRecoveryTokenPurpose,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };

    public AuthenticationChallenge CreatePhoneLoginChallenge(string userId, string phoneNumber, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhoneLogin, userId, phoneNumber, tokenPurpose, now);

    public AuthenticationChallenge CreatePasswordLoginOtpChallenge(string userId, string phoneNumber, string tokenPurpose, DateTimeOffset now, bool rememberMe, bool requiresProfileCompletion) =>
        CreateChallenge(AuthenticationChallengePurpose.PasswordLoginOtp, userId, phoneNumber, tokenPurpose, now, rememberMe, requiresProfileCompletion);

    public AuthenticationChallenge CreateEmailLoginChallenge(string userId, string email, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailLogin, userId, email, tokenPurpose, now);

    public AuthenticationChallenge CreatePhoneChangeChallenge(string userId, string phoneNumber, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhoneChange, userId, phoneNumber, tokenPurpose, now);

    public AuthenticationChallenge CreateEmailChangeChallenge(string userId, string email, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailChange, userId, email, tokenPurpose, now);

    public AuthenticationChallenge CreateEmailPasswordRecoveryChallenge(string userId, string email, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailPasswordRecovery, userId, email, tokenPurpose, now);

    public AuthenticationChallenge CreatePhonePasswordRecoveryChallenge(string userId, string phoneNumber, string tokenPurpose, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhonePasswordRecovery, userId, phoneNumber, tokenPurpose, now);

    public AuthenticationChallenge CreatePasskeyLoginChallenge(string userIdOrLoginHint, string challengeReference, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PasskeyLogin, userIdOrLoginHint, challengeReference, PasskeyTokenPurpose, now);

    private AuthenticationChallenge CreateChallenge(AuthenticationChallengePurpose purpose, string userId, string destination, string tokenPurpose, DateTimeOffset now, bool rememberMe = false, bool requiresProfileCompletion = false) =>
        new(
            new AuthenticationChallengeId(Guid.NewGuid().ToString("N")),
            purpose,
            userId,
            destination,
            tokenPurpose,
            now,
            now.Add(Lifetime),
            MaximumAttempts,
            rememberMe,
            requiresProfileCompletion);
}
