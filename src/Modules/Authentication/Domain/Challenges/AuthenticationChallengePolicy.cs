namespace Vitarish.Authentication.Domain.Challenges;

public sealed record AuthenticationChallengePolicy(TimeSpan Lifetime, int MaximumAttempts)
{
    public static AuthenticationChallengePolicy Default { get; } = new(TimeSpan.FromMinutes(5), 5);
    public static TimeSpan PasswordRecoveryMinimumResponseDuration { get; } = TimeSpan.FromSeconds(1);
    public static TimeSpan PasswordRecoveryDeliveryTimeout { get; } = TimeSpan.FromMinutes(5);
    public const int SecondFactorMaximumAttempts = 1;

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

    public AuthenticationChallenge CreatePhoneLoginChallenge(string userId, string phoneNumber, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhoneLogin, userId, phoneNumber, now);

    public AuthenticationChallenge CreatePasswordLoginOtpChallenge(string userId, string phoneNumber, DateTimeOffset now, bool requiresProfileCompletion) =>
        CreateChallenge(AuthenticationChallengePurpose.PasswordLoginOtp, userId, phoneNumber, now, requiresProfileCompletion, SecondFactorMaximumAttempts);

    public AuthenticationChallenge CreateEmailLoginChallenge(string userId, string email, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailLogin, userId, email, now);

    public AuthenticationChallenge CreatePhoneChangeChallenge(string userId, string phoneNumber, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhoneChange, userId, phoneNumber, now);

    public AuthenticationChallenge CreateEmailChangeChallenge(string userId, string email, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailChange, userId, email, now);

    public AuthenticationChallenge CreateEmailPasswordRecoveryChallenge(string userId, string email, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.EmailPasswordRecovery, userId, email, now);

    public AuthenticationChallenge CreatePhonePasswordRecoveryChallenge(string userId, string phoneNumber, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PhonePasswordRecovery, userId, phoneNumber, now);

    public AuthenticationChallenge CreatePasskeyLoginChallenge(string userIdOrLoginHint, string challengeReference, DateTimeOffset now) =>
        CreateChallenge(AuthenticationChallengePurpose.PasskeyLogin, userIdOrLoginHint, challengeReference, now);

    private AuthenticationChallenge CreateChallenge(AuthenticationChallengePurpose purpose, string userId, string destination, DateTimeOffset now, bool requiresProfileCompletion = false, int? maximumAttempts = null)
    {
        AuthenticationChallengeId challengeId = new(Guid.NewGuid().ToString("N"));
        return new(
            challengeId,
            purpose,
            userId,
            destination,
            TokenPurpose(purpose, challengeId),
            now,
            now.Add(Lifetime),
            maximumAttempts ?? MaximumAttempts,
            requiresProfileCompletion);
    }

    private static string TokenPurpose(AuthenticationChallengePurpose purpose, AuthenticationChallengeId challengeId) =>
        $"{BaseTokenPurpose(purpose)}:{challengeId.Value}";

    private static string BaseTokenPurpose(AuthenticationChallengePurpose purpose) => purpose switch
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
}
