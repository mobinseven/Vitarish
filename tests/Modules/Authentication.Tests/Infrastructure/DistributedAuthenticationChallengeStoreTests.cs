using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class DistributedAuthenticationChallengeStoreTests
{
    [DockerFact]
    public async Task Redis_scripts_limit_attempts_and_exclusively_complete_password_recovery_challenges()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();

        await using (redis)
        {
            await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
            MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
            DistributedAuthenticationChallengeStore store = new(cache, multiplexer);
            AuthenticationChallenge failedChallenge = CreateChallenge();
            await store.SaveAsync(failedChallenge, CancellationToken.None);

            AuthenticationChallenge[] requests = Enumerable.Range(0, 32)
                .Select(_ => Copy(failedChallenge))
                .ToArray();
            bool[] reservations = await Task.WhenAll(
                requests.Select(request => store.TryReserveAttemptAsync(request, CancellationToken.None)));

            reservations.Count(result => result).Should().Be(failedChallenge.MaximumAttempts);

            int[] failures = await Task.WhenAll(
                requests.Where((_, index) => reservations[index])
                    .Select(request => store.TryRegisterFailureAsync(request, CancellationToken.None)));

            failures.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
            (await store.GetAsync(failedChallenge.Id, CancellationToken.None))!.Status
                .Should().Be(AuthenticationChallengeStatus.Exhausted);

            AuthenticationChallenge successfulChallenge = CreateChallenge();
            await store.SaveAsync(successfulChallenge, CancellationToken.None);

            (await store.TryReserveAttemptAsync(successfulChallenge, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(successfulChallenge, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(successfulChallenge, CancellationToken.None)).Should().BeFalse();
            (await store.GetAsync(successfulChallenge.Id, CancellationToken.None))!.Status
                .Should().Be(AuthenticationChallengeStatus.Completed);

            AuthenticationChallenge recoveryChallenge = CreateRecoveryChallenge();
            await store.SaveAsync(recoveryChallenge, CancellationToken.None);
            (await store.TryReserveAttemptAsync(recoveryChallenge, CancellationToken.None)).Should().BeTrue();
            (await store.TryReserveCompletionAsync(recoveryChallenge, CancellationToken.None)).Should().BeTrue();
            await store.ReleaseCompletionAsync(recoveryChallenge, CancellationToken.None);
            (await store.TryReserveCompletionAsync(recoveryChallenge, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(recoveryChallenge, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(recoveryChallenge, CancellationToken.None)).Should().BeFalse();

            AuthenticationChallenge concurrentRecovery = CreateRecoveryChallenge();
            await store.SaveAsync(concurrentRecovery, CancellationToken.None);
            (await store.TryReserveAttemptAsync(concurrentRecovery, CancellationToken.None)).Should().BeTrue();
            (await store.TryReserveAttemptAsync(concurrentRecovery, CancellationToken.None)).Should().BeTrue();
            bool[] completionReservations = await Task.WhenAll(
                Enumerable.Range(0, 32)
                    .Select(_ => store.TryReserveCompletionAsync(concurrentRecovery, CancellationToken.None)));

            completionReservations.Count(reserved => reserved).Should().Be(1);
            (await store.TryReserveAttemptAsync(concurrentRecovery, CancellationToken.None)).Should().BeFalse();
            (await store.TryRegisterFailureAsync(concurrentRecovery, CancellationToken.None)).Should().Be(0);
            (await store.TryConsumeAsync(concurrentRecovery, CancellationToken.None)).Should().BeTrue();

            AuthenticationChallenge unreservedRecovery = CreateRecoveryChallenge();
            await store.SaveAsync(unreservedRecovery, CancellationToken.None);
            (await store.TryReserveAttemptAsync(unreservedRecovery, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(unreservedRecovery, CancellationToken.None)).Should().BeFalse();
            (await store.TryReserveAttemptAsync(unreservedRecovery, CancellationToken.None)).Should().BeTrue();
            (await store.TryReserveCompletionAsync(unreservedRecovery, CancellationToken.None)).Should().BeTrue();
            (await store.TryConsumeAsync(unreservedRecovery, CancellationToken.None)).Should().BeTrue();
        }
    }

    private static AuthenticationChallenge CreateChallenge() => AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
        "user-42",
        "+989121234567",
        "Authentication:PasswordLoginOtp",
        DateTimeOffset.UtcNow,
        rememberMe: false,
        requiresProfileCompletion: false);

    private static AuthenticationChallenge CreateRecoveryChallenge() => AuthenticationChallengePolicy.Default.CreateEmailPasswordRecoveryChallenge(
        "user-42",
        "person@example.test",
        AuthenticationChallengePolicy.Default.TokenPurpose(AuthenticationChallengePurpose.EmailPasswordRecovery),
        DateTimeOffset.UtcNow);

    private static AuthenticationChallenge Copy(AuthenticationChallenge challenge) => AuthenticationChallenge.Restore(
        challenge.Id,
        challenge.Purpose,
        challenge.UserId,
        challenge.Destination,
        challenge.TokenPurpose,
        challenge.CreatedAt,
        challenge.ExpiresAt,
        challenge.MaximumAttempts,
        challenge.FailedAttempts,
        challenge.Status,
        challenge.CompletedAt,
        challenge.RememberMe,
        challenge.RequiresProfileCompletion);

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (!IsDockerAvailable())
                Skip = "Redis integration coverage requires an available Docker engine.";
        }

        private static bool IsDockerAvailable()
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")))
                return true;

            return OperatingSystem.IsWindows()
                ? File.Exists(@"\\.\pipe\docker_engine") || File.Exists(@"\\.\pipe\dockerDesktopLinuxEngine")
                : File.Exists("/var/run/docker.sock");
        }
    }
}
