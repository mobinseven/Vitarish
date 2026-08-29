using FluentAssertions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Testcontainers.Redis;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class RedisAuthenticationAttemptTrackerTests
{
    [DockerFact]
    public async Task Redis_uses_the_same_fixed_failure_window_as_in_memory()
    {
        await using RedisContainer redis = new RedisBuilder("redis:7.4-alpine").Build();
        await redis.StartAsync();
        await using ConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());
        RedisAuthenticationAttemptTracker tracker = new(multiplexer);
        string key = new AuthenticationAttemptKeyFactory(Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "redis-tests",
            CurrentPepper = "redis-authentication-attempt-key-pepper-20260829"
        })).Identifier(AuthenticationMethod.Password, "window-user");

        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromSeconds(3), CancellationToken.None)).Should().Be(1);
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromSeconds(3), CancellationToken.None)).Should().Be(2);
        await Task.Delay(TimeSpan.FromSeconds(2.1));

        (await tracker.RegisterFailureAsync(key, 3, TimeSpan.FromSeconds(3), CancellationToken.None)).Should().Be(1);
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (!IsDockerAvailable())
                Skip = "Redis integration coverage requires an available Docker engine.";
        }

        private static bool IsDockerAvailable() =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOCKER_HOST")) ||
            (OperatingSystem.IsWindows()
                ? File.Exists(@"\\.\pipe\docker_engine") || File.Exists(@"\\.\pipe\dockerDesktopLinuxEngine")
                : File.Exists("/var/run/docker.sock"));
    }
}
