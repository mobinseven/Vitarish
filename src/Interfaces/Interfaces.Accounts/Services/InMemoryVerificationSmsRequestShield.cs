using Microsoft.Extensions.Options;

namespace Vitarish.Interfaces.Accounts.Services;

public sealed class InMemoryVerificationSmsRequestShield(
    IOptions<VerificationSmsShieldOptions> options,
    TimeProvider timeProvider) : IVerificationSmsRequestShield
{
    private readonly object syncRoot = new();
    private readonly Dictionary<string, VerificationSmsRequest> recentRequests = new(StringComparer.Ordinal);
    private DateTimeOffset lastCleanupAt = DateTimeOffset.MinValue;

    internal int RecentRequestCount
    {
        get
        {
            lock (syncRoot)
            {
                return recentRequests.Count;
            }
        }
    }

    public VerificationSmsShieldDecision TryAcquireSendSlot(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Verification SMS shield key is required.", nameof(key));
        }

        VerificationSmsShieldOptions currentOptions = options.Value;
        ValidateOptions(currentOptions);
        DateTimeOffset now = timeProvider.GetUtcNow();

        lock (syncRoot)
        {
            RemoveExpiredRequestsWhenDue(now, currentOptions.CleanupInterval);

            if (recentRequests.TryGetValue(key, out VerificationSmsRequest? existingRequest))
            {
                if (existingRequest.ExpiresAt > now)
                {
                    return VerificationSmsShieldDecision.Shielded(existingRequest.ExpiresAt - now);
                }

                _ = recentRequests.Remove(key);
            }

            EnsureCapacity(currentOptions.VerificationSmsMaximumRecentRequestCount);

            DateTimeOffset expiresAt = now.Add(currentOptions.Cooldown);
            recentRequests[key] = new VerificationSmsRequest(key, now, expiresAt);
            return VerificationSmsShieldDecision.Allowed;
        }
    }

    public void ReleaseSendSlot(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Verification SMS shield key is required.", nameof(key));
        }

        lock (syncRoot)
        {
            _ = recentRequests.Remove(key);
        }
    }

    private static void ValidateOptions(VerificationSmsShieldOptions currentOptions)
    {
        if (currentOptions.VerificationSmsCooldownSeconds <= 0)
        {
            throw new InvalidOperationException("Verification SMS cooldown must be greater than zero seconds.");
        }

        if (currentOptions.VerificationSmsRecentRequestCleanupIntervalSeconds <= 0)
        {
            throw new InvalidOperationException("Verification SMS recent request cleanup interval must be greater than zero seconds.");
        }

        if (currentOptions.VerificationSmsMaximumRecentRequestCount <= 0)
        {
            throw new InvalidOperationException("Verification SMS maximum recent request count must be greater than zero when configured.");
        }
    }

    private void RemoveExpiredRequestsWhenDue(DateTimeOffset now, TimeSpan cleanupInterval)
    {
        if (lastCleanupAt != DateTimeOffset.MinValue && now - lastCleanupAt < cleanupInterval)
        {
            return;
        }

        RemoveExpiredRequests(now);
        lastCleanupAt = now;
    }

    private void RemoveExpiredRequests(DateTimeOffset now)
    {
        string[] expiredKeys = [.. recentRequests.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key)];
        foreach (string expiredKey in expiredKeys)
        {
            _ = recentRequests.Remove(expiredKey);
        }
    }

    private void EnsureCapacity(int? maximumRecentRequestCount)
    {
        if (maximumRecentRequestCount is null || recentRequests.Count < maximumRecentRequestCount.Value)
        {
            return;
        }

        string oldestKey = recentRequests
            .OrderBy(pair => pair.Value.ExpiresAt)
            .Select(pair => pair.Key)
            .First();
        _ = recentRequests.Remove(oldestKey);
    }
}
