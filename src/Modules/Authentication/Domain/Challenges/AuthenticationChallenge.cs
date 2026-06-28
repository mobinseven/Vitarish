namespace Vitarish.Authentication.Domain.Challenges;

public sealed class AuthenticationChallenge
{
    public AuthenticationChallenge(AuthenticationChallengeId id, AuthenticationChallengePurpose purpose, DateTimeOffset createdAt, DateTimeOffset expiresAt, int maximumAttempts)
    { Id = id; Purpose = purpose; CreatedAt = createdAt; ExpiresAt = expiresAt; MaximumAttempts = maximumAttempts; }
    public AuthenticationChallengeId Id { get; }
    public AuthenticationChallengePurpose Purpose { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public int MaximumAttempts { get; }
    public int FailedAttempts { get; private set; }
    public AuthenticationChallengeStatus Status { get; private set; } = AuthenticationChallengeStatus.Pending;
    public int RemainingAttempts => Math.Max(0, MaximumAttempts - FailedAttempts);
    public bool CanAttempt(DateTimeOffset now) => Status == AuthenticationChallengeStatus.Pending && now < ExpiresAt && RemainingAttempts > 0;
    public void RegisterFailure(DateTimeOffset now) { if (!CanAttempt(now)) { Status = now >= ExpiresAt ? AuthenticationChallengeStatus.Expired : AuthenticationChallengeStatus.Exhausted; return; } FailedAttempts++; if (RemainingAttempts == 0) Status = AuthenticationChallengeStatus.Exhausted; }
    public void Complete(DateTimeOffset now) { if (!CanAttempt(now)) throw new InvalidOperationException("Challenge cannot be completed."); Status = AuthenticationChallengeStatus.Completed; }
}
