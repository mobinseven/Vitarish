using System.Collections.Concurrent;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class InMemoryAuthenticationChallengeStore : IAuthenticationChallengeStore
{
    private readonly ConcurrentDictionary<string, AuthenticationChallenge> challenges = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> consumed = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> completions = new();
    private readonly ConcurrentDictionary<string, int> reservations = new();
    private readonly ConcurrentDictionary<string, object> gates = new();

    public Task SaveAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenges[challenge.Id.Value] = challenge;
        return Task.CompletedTask;
    }

    public Task<AuthenticationChallenge?> GetAsync(AuthenticationChallengeId id, CancellationToken cancellationToken)
    {
        challenges.TryGetValue(id.Value, out AuthenticationChallenge? c);
        if (c is { Status: AuthenticationChallengeStatus.Pending } && consumed.TryGetValue(id.Value, out DateTimeOffset expiresAt))
        {
            if (expiresAt > DateTimeOffset.UtcNow)
                c.Complete(DateTimeOffset.UtcNow);
            else
                _ = consumed.TryRemove(id.Value, out _);
        }

        return Task.FromResult(c);
    }

    public Task ReplaceAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenges[challenge.Id.Value] = challenge;
        return Task.CompletedTask;
    }

    public Task<bool> TryReserveAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            if (!challenges.TryGetValue(challenge.Id.Value, out AuthenticationChallenge? current))
                return Task.FromResult(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (consumed.TryGetValue(challenge.Id.Value, out DateTimeOffset consumedUntil))
            {
                if (consumedUntil > now)
                    return Task.FromResult(false);

                _ = consumed.TryRemove(challenge.Id.Value, out _);
            }

            if (!current.CanAttempt(now))
                return Task.FromResult(false);

            if (IsCompletionReserved(challenge.Id.Value, now))
                return Task.FromResult(false);

            reservations.TryGetValue(challenge.Id.Value, out int reserved);
            if (current.FailedAttempts + reserved >= current.MaximumAttempts)
                return Task.FromResult(false);

            reservations[challenge.Id.Value] = reserved + 1;
            return Task.FromResult(true);
        }
    }

    public Task ReleaseAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            ReleaseReservation(challenge.Id.Value);
            return Task.CompletedTask;
        }
    }

    public Task<bool> TryReserveCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            if (!challenges.TryGetValue(challenge.Id.Value, out AuthenticationChallenge? current))
                return Task.FromResult(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!current.CanAttempt(now) ||
                !reservations.TryGetValue(challenge.Id.Value, out int reserved) ||
                reserved <= 0 ||
                IsCompletionReserved(challenge.Id.Value, now))
                return Task.FromResult(false);

            if (consumed.TryGetValue(challenge.Id.Value, out DateTimeOffset consumedUntil) && consumedUntil > now)
                return Task.FromResult(false);

            completions[challenge.Id.Value] = current.ExpiresAt;
            return Task.FromResult(true);
        }
    }

    public Task ReleaseCompletionAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            _ = completions.TryRemove(challenge.Id.Value, out _);
            return Task.CompletedTask;
        }
    }

    public Task<bool> TryConsumeAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            if (!challenges.TryGetValue(challenge.Id.Value, out AuthenticationChallenge? current))
                return Task.FromResult(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (consumed.TryGetValue(challenge.Id.Value, out DateTimeOffset consumedUntil))
            {
                if (consumedUntil > now)
                {
                    ReleaseReservation(challenge.Id.Value);
                    _ = completions.TryRemove(challenge.Id.Value, out _);
                    return Task.FromResult(false);
                }

                _ = consumed.TryRemove(challenge.Id.Value, out _);
            }

            if (!current.CanAttempt(now))
            {
                ReleaseReservation(challenge.Id.Value);
                return Task.FromResult(false);
            }

            if (IsPasswordRecovery(challenge.Purpose) && !IsCompletionReserved(challenge.Id.Value, now))
            {
                ReleaseReservation(challenge.Id.Value);
                return Task.FromResult(false);
            }

            if (!consumed.TryAdd(challenge.Id.Value, current.ExpiresAt))
            {
                ReleaseReservation(challenge.Id.Value);
                return Task.FromResult(false);
            }

            ReleaseReservation(challenge.Id.Value);
            _ = completions.TryRemove(challenge.Id.Value, out _);
            current.Complete(now);
            return Task.FromResult(true);
        }
    }

    public Task<int> TryRegisterFailureAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gates.GetOrAdd(challenge.Id.Value, static _ => new object()))
        {
            if (!challenges.TryGetValue(challenge.Id.Value, out AuthenticationChallenge? current))
                return Task.FromResult(0);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (consumed.TryGetValue(challenge.Id.Value, out DateTimeOffset consumedUntil))
            {
                if (consumedUntil > now)
                {
                    ReleaseReservation(challenge.Id.Value);
                    return Task.FromResult(0);
                }

                _ = consumed.TryRemove(challenge.Id.Value, out _);
            }

            if (IsCompletionReserved(challenge.Id.Value, now))
            {
                ReleaseReservation(challenge.Id.Value);
                return Task.FromResult(0);
            }

            if (!current.CanAttempt(now))
            {
                ReleaseReservation(challenge.Id.Value);
                return Task.FromResult(0);
            }

            if (!reservations.TryGetValue(challenge.Id.Value, out int reserved) || reserved <= 0)
                return Task.FromResult(0);

            ReleaseReservation(challenge.Id.Value);
            current.RegisterFailure(now);
            return Task.FromResult(current.FailedAttempts);
        }
    }

    private void ReleaseReservation(string challengeId)
    {
        if (!reservations.TryGetValue(challengeId, out int reserved) || reserved <= 1)
            _ = reservations.TryRemove(challengeId, out _);
        else
            reservations[challengeId] = reserved - 1;
    }

    private bool IsCompletionReserved(string challengeId, DateTimeOffset now)
    {
        if (!completions.TryGetValue(challengeId, out DateTimeOffset expiresAt))
            return false;
        if (expiresAt > now)
            return true;

        _ = completions.TryRemove(challengeId, out _);
        return false;
    }

    private static bool IsPasswordRecovery(AuthenticationChallengePurpose purpose) =>
        purpose is AuthenticationChallengePurpose.EmailPasswordRecovery or AuthenticationChallengePurpose.PhonePasswordRecovery;
}
