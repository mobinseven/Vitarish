namespace Vitarish.Authentication.Domain.Challenges;

public sealed record AuthenticationChallengePolicy(TimeSpan Lifetime, int MaximumAttempts)
{
    public static AuthenticationChallengePolicy Default { get; } = new(TimeSpan.FromMinutes(5), 5);
    public bool IsExpired(DateTimeOffset createdAt, DateTimeOffset now) => now >= createdAt.Add(Lifetime);
    public bool HasAttemptsRemaining(int failedAttempts) => failedAttempts < MaximumAttempts;
}
