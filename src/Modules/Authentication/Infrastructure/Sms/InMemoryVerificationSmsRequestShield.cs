using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Vitarish.Authentication.Infrastructure;

public sealed class InMemoryVerificationSmsRequestShield(
    IOptions<VerificationShieldOptions> options) : VerificationSmsRequestShield
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> recentRequests = new(StringComparer.Ordinal);
    private DateTimeOffset lastCleanupAt = DateTimeOffset.MinValue;

    public override bool CanSend(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        VerificationShieldOptions currentOptions = options.Value;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string key = NormalizeKey(phoneNumber);

        CleanupExpiredEntries(now, currentOptions);

        List<DateTimeOffset> timestamps = recentRequests.GetOrAdd(key, _ => []);

        lock (timestamps)
        {
            RemoveExpiredTimestamps(timestamps, now, currentOptions);

            if (timestamps.Count >= currentOptions.MaximumRequests)
                return false;

            timestamps.Add(now);
            return true;
        }
    }

    private void CleanupExpiredEntries(DateTimeOffset now, VerificationShieldOptions currentOptions)
    {
        TimeSpan cleanupInterval = TimeSpan.FromMinutes(currentOptions.WindowMinutes);
        if (lastCleanupAt != DateTimeOffset.MinValue && now - lastCleanupAt < cleanupInterval)
            return;

        foreach (KeyValuePair<string, List<DateTimeOffset>> entry in recentRequests)
        {
            lock (entry.Value)
            {
                entry.Value.RemoveAll(ts => ts + TimeSpan.FromMinutes(currentOptions.WindowMinutes) <= now);
            }

            if (entry.Value.Count == 0)
                recentRequests.TryRemove(entry.Key, out _);
        }

        lastCleanupAt = now;
    }

    private static void RemoveExpiredTimestamps(List<DateTimeOffset> timestamps, DateTimeOffset now, VerificationShieldOptions currentOptions)
    {
        timestamps.RemoveAll(ts => ts + TimeSpan.FromMinutes(currentOptions.WindowMinutes) <= now);
    }

    private static string NormalizeKey(string phoneNumber) =>
        new([.. phoneNumber.Where(static c => char.IsDigit(c) || c == '+')]);
}
