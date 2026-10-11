using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Domain.Challenges;

public sealed class AuthenticationChallenge
{
    public AuthenticationChallenge(
        AuthenticationChallengeId id,
        AuthenticationChallengePurpose purpose,
        string userId,
        string destination,
        string tokenPurpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int maximumAttempts,
        bool requiresProfileCompletion = false)
        : this(id, purpose, userId, destination, tokenPurpose, createdAt, expiresAt, maximumAttempts, 0, AuthenticationChallengeStatus.Pending, null, requiresProfileCompletion)
    {
    }

    private AuthenticationChallenge(
        AuthenticationChallengeId id,
        AuthenticationChallengePurpose purpose,
        string userId,
        string destination,
        string tokenPurpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int maximumAttempts,
        int failedAttempts,
        AuthenticationChallengeStatus status,
        DateTimeOffset? completedAt,
        bool requiresProfileCompletion)
    {
        if (maximumAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Maximum attempts must be at least one.");
        if (failedAttempts < 0 || failedAttempts > maximumAttempts) throw new ArgumentOutOfRangeException(nameof(failedAttempts), "Failed attempts must be within challenge attempt bounds.");

        Id = id;
        Purpose = purpose;
        UserId = userId;
        Destination = destination;
        TokenPurpose = tokenPurpose;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        MaximumAttempts = maximumAttempts;
        FailedAttempts = failedAttempts;
        Status = status;
        CompletedAt = completedAt;
        RequiresProfileCompletion = requiresProfileCompletion;
    }

    public AuthenticationChallengeId Id { get; }
    public AuthenticationChallengePurpose Purpose { get; }
    public string UserId { get; }
    public string Destination { get; }
    public string TokenPurpose { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int MaximumAttempts { get; }
    public int FailedAttempts { get; private set; }
    public AuthenticationChallengeStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool RequiresProfileCompletion { get; }
    public int RemainingAttempts => Math.Max(0, MaximumAttempts - FailedAttempts);

    /// <summary>
    /// The second step of a sign-in. One wrong code ends it, and the user restarts
    /// from the first factor (NSMAIL-1 FIA_UAU.5).
    /// </summary>
    public bool IsSecondFactor => Purpose is AuthenticationChallengePurpose.PasswordLoginOtp;

    public static AuthenticationChallenge Restore(
        AuthenticationChallengeId id,
        AuthenticationChallengePurpose purpose,
        string userId,
        string destination,
        string tokenPurpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int maximumAttempts,
        int failedAttempts,
        AuthenticationChallengeStatus status,
        DateTimeOffset? completedAt,
        bool requiresProfileCompletion = false) => new(id, purpose, userId, destination, tokenPurpose, createdAt, expiresAt, maximumAttempts, failedAttempts, status, completedAt, requiresProfileCompletion);

    public bool CanAttempt(DateTimeOffset now) => Status == AuthenticationChallengeStatus.Pending && now < ExpiresAt && RemainingAttempts > 0;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public AuthenticationFailureReason? ValidateAttempt(DateTimeOffset now, AuthenticationChallengePurpose expectedPurpose)
    {
        if (Purpose != expectedPurpose) return AuthenticationFailureReason.InvalidInput;
        if (Status == AuthenticationChallengeStatus.Completed) return AuthenticationFailureReason.ChallengeAlreadyUsed;
        if (Status == AuthenticationChallengeStatus.Expired || now >= ExpiresAt) return AuthenticationFailureReason.ChallengeExpired;
        if (Status == AuthenticationChallengeStatus.Exhausted) return AuthenticationFailureReason.ChallengeAttemptsExhausted;
        if (Status != AuthenticationChallengeStatus.Pending) return AuthenticationFailureReason.InvalidInput;
        return null;
    }

    public void RegisterFailure(DateTimeOffset now)
    {
        if (Status == AuthenticationChallengeStatus.Completed)
        {
            return;
        }

        if (now >= ExpiresAt)
        {
            Status = AuthenticationChallengeStatus.Expired;
            return;
        }

        if (Status != AuthenticationChallengeStatus.Pending)
        {
            return;
        }

        FailedAttempts++;
        if (RemainingAttempts == 0)
        {
            Status = AuthenticationChallengeStatus.Exhausted;
        }
    }

    public void Cancel()
    {
        if (Status == AuthenticationChallengeStatus.Pending)
        {
            Status = AuthenticationChallengeStatus.Cancelled;
        }
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status == AuthenticationChallengeStatus.Completed) throw new InvalidOperationException("Challenge has already been completed.");
        if (Status == AuthenticationChallengeStatus.Expired || now >= ExpiresAt) throw new InvalidOperationException("Challenge has expired.");
        if (Status == AuthenticationChallengeStatus.Exhausted) throw new InvalidOperationException("Challenge attempts have been exhausted.");
        if (Status != AuthenticationChallengeStatus.Pending) throw new InvalidOperationException("Challenge cannot be completed in its current state.");

        Status = AuthenticationChallengeStatus.Completed;
        CompletedAt = now;
    }
}
