using FluentAssertions;
using Microsoft.Extensions.Options;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class AuthenticationAttemptTrackerTests
{
    [Fact]
    public async Task Tracks_normalized_identifiers_until_the_threshold_then_denies()
    {
        InMemoryAuthenticationAttemptTracker tracker = new();

        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        string key = keys.Identifier(AuthenticationMethod.Password, "User-1");
        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None)).Should().Be(1);
        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None)).Should().Be(2);
        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None)).Should().Be(3);
        (await tracker.IsDeniedAsync(key, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Reset_removes_denial_state()
    {
        InMemoryAuthenticationAttemptTracker tracker = new();

        string key = CreateKeys().Identifier(AuthenticationMethod.Password, "user-1");
        _ = await tracker.RegisterFailureAsync(key, 1, TimeSpan.FromMinutes(1), CancellationToken.None);
        await tracker.ResetAsync(key, CancellationToken.None);

        (await tracker.IsDeniedAsync(key, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Subject_key_is_shared_across_login_aliases()
    {
        InMemoryAuthenticationAttemptTracker tracker = new();
        string subjectKey = CreateKeys().Subject(AuthenticationMethod.Password, "user-1");

        _ = await tracker.RegisterFailureAsync(subjectKey, 2, TimeSpan.FromMinutes(1), CancellationToken.None);
        _ = await tracker.RegisterFailureAsync(subjectKey, 2, TimeSpan.FromMinutes(1), CancellationToken.None);

        (await tracker.IsDeniedAsync(subjectKey, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Fixed_failure_window_does_not_slide_after_each_failure()
    {
        ManualTimeProvider clock = new(DateTimeOffset.UtcNow);
        InMemoryAuthenticationAttemptTracker tracker = new(clock);
        string key = CreateKeys().Identifier(AuthenticationMethod.Password, "window-user");

        _ = await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(50));
        _ = await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(11));

        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task Denial_is_retained_for_the_full_duration_when_threshold_is_reached_late()
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;
        ManualTimeProvider clock = new(start);
        InMemoryAuthenticationAttemptTracker tracker = new(clock);
        string key = CreateKeys().Identifier(AuthenticationMethod.Password, "late-threshold-user");

        _ = await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(59));
        _ = await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None);
        _ = await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromMinutes(1), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(2));
        (await tracker.IsDeniedAsync(key, CancellationToken.None)).Should().BeTrue();
        clock.Advance(TimeSpan.FromSeconds(58));
        (await tracker.IsDeniedAsync(key, CancellationToken.None)).Should().BeFalse();
    }

    private static IAuthenticationAttemptKeyFactory CreateKeys() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

    private sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset now = initial;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now = now.Add(duration);
    }
}
