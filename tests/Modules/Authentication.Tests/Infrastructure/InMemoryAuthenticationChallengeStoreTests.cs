using FluentAssertions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class InMemoryAuthenticationChallengeStoreTests
{
    [Fact]
    public async Task TryConsumeAsync_allows_only_one_concurrent_consumer()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42",
            "+989121234567",
            DateTimeOffset.UtcNow,
            rememberMe: false,
            requiresProfileCompletion: false);
        await store.SaveAsync(challenge, CancellationToken.None);

        AuthenticationChallenge[] requests = Enumerable.Range(0, 32)
            .Select(_ => AuthenticationChallenge.Restore(
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
                challenge.RequiresProfileCompletion))
            .ToArray();

        bool[] results = await Task.WhenAll(
            requests.Select(request => store.TryConsumeAsync(request, CancellationToken.None)));

        results.Count(result => result).Should().Be(1);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Completed);
    }

    [Fact]
    public async Task Completion_reservation_is_exclusive_and_blocks_other_attempts_until_released_or_consumed()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailPasswordRecoveryChallenge(
            "user-42",
            "person@example.test",
            DateTimeOffset.UtcNow);
        await store.SaveAsync(challenge, CancellationToken.None);
        (await store.TryReserveAttemptAsync(challenge, CancellationToken.None)).Should().BeTrue();
        (await store.TryConsumeAsync(challenge, CancellationToken.None)).Should().BeFalse();
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status.Should().Be(AuthenticationChallengeStatus.Pending);
        (await store.TryReserveAttemptAsync(challenge, CancellationToken.None)).Should().BeTrue();
        (await store.TryReserveAttemptAsync(challenge, CancellationToken.None)).Should().BeTrue();

        Task<bool>[] completions = Enumerable.Range(0, 16)
            .Select(_ => store.TryReserveCompletionAsync(challenge, CancellationToken.None))
            .ToArray();
        bool[] reservations = await Task.WhenAll(completions);

        reservations.Count(reserved => reserved).Should().Be(1);
        (await store.TryReserveAttemptAsync(challenge, CancellationToken.None)).Should().BeFalse();
        (await store.TryRegisterFailureAsync(challenge, CancellationToken.None)).Should().Be(0);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.FailedAttempts.Should().Be(0);

        await store.ReleaseCompletionAsync(challenge, CancellationToken.None);
        (await store.TryReserveCompletionAsync(challenge, CancellationToken.None)).Should().BeTrue();
        (await store.TryConsumeAsync(challenge, CancellationToken.None)).Should().BeTrue();
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status.Should().Be(AuthenticationChallengeStatus.Completed);
    }

    [Fact]
    public async Task TryRegisterFailureAsync_allows_only_the_configured_number_of_concurrent_failures()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePhoneLoginChallenge(
            "user-42",
            "+989121234567",
            DateTimeOffset.UtcNow);
        await store.SaveAsync(challenge, CancellationToken.None);

        AuthenticationChallenge[] requests = Enumerable.Range(0, 32)
            .Select(_ => AuthenticationChallenge.Restore(
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
                challenge.RequiresProfileCompletion))
            .ToArray();

        bool[] reservations = await Task.WhenAll(
            requests.Select(request => store.TryReserveAttemptAsync(request, CancellationToken.None)));

        reservations.Count(result => result).Should().Be(challenge.MaximumAttempts);

        int[] results = await Task.WhenAll(
            requests.Where((_, index) => reservations[index])
                .Select(request => store.TryRegisterFailureAsync(request, CancellationToken.None)));

        results.Count(result => result > 0).Should().Be(challenge.MaximumAttempts);
        results.Where(result => result > 0).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Exhausted);
    }
}
