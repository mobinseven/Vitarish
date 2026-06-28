namespace Vitarish.Authentication.Domain.Challenges;

public sealed class AuthenticationChallenge
{
    public AuthenticationChallenge(AuthenticationChallengeId id, AuthenticationChallengePurpose purpose, DateTimeOffset createdAt, DateTimeOffset expiresAt, int maximumAttempts)
        : this(id, purpose, createdAt, expiresAt, maximumAttempts, 0, AuthenticationChallengeStatus.Pending)
    {
    }

    private AuthenticationChallenge(
        AuthenticationChallengeId id,
        AuthenticationChallengePurpose purpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int maximumAttempts,
        int failedAttempts,
        AuthenticationChallengeStatus status)
    {
        if (maximumAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "Maximum attempts must be at least one.");
        if (failedAttempts < 0 || failedAttempts > maximumAttempts) throw new ArgumentOutOfRangeException(nameof(failedAttempts), "Failed attempts must be within challenge attempt bounds.");

        Id = id;
        Purpose = purpose;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        MaximumAttempts = maximumAttempts;
        FailedAttempts = failedAttempts;
        Status = status;
    }

    public AuthenticationChallengeId Id { get; }
    public AuthenticationChallengePurpose Purpose { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int MaximumAttempts { get; }
    public int FailedAttempts { get; private set; }
    public AuthenticationChallengeStatus Status { get; private set; }
    public int RemainingAttempts => Math.Max(0, MaximumAttempts - FailedAttempts);

    public static AuthenticationChallenge Restore(
        AuthenticationChallengeId id,
        AuthenticationChallengePurpose purpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        int maximumAttempts,
        int failedAttempts,
        AuthenticationChallengeStatus status) => new(id, purpose, createdAt, expiresAt, maximumAttempts, failedAttempts, status);

    public bool CanAttempt(DateTimeOffset now) => Status == AuthenticationChallengeStatus.Pending && now < ExpiresAt && RemainingAttempts > 0;

    public void RegisterFailure(DateTimeOffset now)
    {
        if (Status == AuthenticationChallengeStatus.Completed)
        {
            return;
        }

        if (!CanAttempt(now))
        {
            Status = now >= ExpiresAt ? AuthenticationChallengeStatus.Expired : AuthenticationChallengeStatus.Exhausted;
            return;
        }

        FailedAttempts++;
        if (RemainingAttempts == 0)
        {
            Status = AuthenticationChallengeStatus.Exhausted;
        }
    }

    public void Complete(DateTimeOffset now)
    {
        if (!CanAttempt(now)) throw new InvalidOperationException("Challenge cannot be completed.");
        Status = AuthenticationChallengeStatus.Completed;
    }
}
