using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;

namespace Vitarish.Authentication.Tests.Infrastructure;

public sealed class PasswordRecoveryCodeDispatcherTests
{
    private static readonly AsyncLocal<string?> AmbientProbe = new();

    [Fact]
    public async Task DispatchAsync_returns_before_a_synchronously_blocking_sender_finishes()
    {
        using ManualResetEventSlim senderBlocked = new();
        TaskCompletionSource deliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource deliveryFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ServiceProvider provider = CreateProvider(emailSender => emailSender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                deliveryStarted.TrySetResult();
                senderBlocked.Wait();
                deliveryFinished.TrySetResult();
                return Task.CompletedTask;
            }));
        PasswordRecoveryCodeDispatcher dispatcher = CreateDispatcher(provider);
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default
            .CreateEmailPasswordRecoveryChallenge("user-42", "person@example.test", DateTimeOffset.UtcNow);

        Task dispatch = Task.Run(() => dispatcher.DispatchAsync(challenge, "test-code", CancellationToken.None));

        try
        {
            await dispatch.WaitAsync(TimeSpan.FromSeconds(5));
            await deliveryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            deliveryFinished.Task.IsCompleted.Should().BeFalse();
        }
        finally
        {
            senderBlocked.Set();
        }

        await deliveryFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DispatchAsync_does_not_flow_the_calling_execution_context()
    {
        TaskCompletionSource<string?> observedAmbient = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ServiceProvider provider = CreateProvider(emailSender => emailSender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                observedAmbient.TrySetResult(AmbientProbe.Value);
                return Task.CompletedTask;
            }));
        PasswordRecoveryCodeDispatcher dispatcher = CreateDispatcher(provider);
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default
            .CreateEmailPasswordRecoveryChallenge("user-42", "person@example.test", DateTimeOffset.UtcNow);

        AmbientProbe.Value = "request-ambient";
        try
        {
            await dispatcher.DispatchAsync(challenge, "test-code", CancellationToken.None);

            (await observedAmbient.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeNull();
        }
        finally
        {
            AmbientProbe.Value = null;
        }
    }

    private static PasswordRecoveryCodeDispatcher CreateDispatcher(ServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<PasswordRecoveryCodeDispatcher>.Instance);

    private static ServiceProvider CreateProvider(Action<Mock<IEmailAuthenticationCodeSender>> configureEmailSender)
    {
        Mock<IEmailAuthenticationCodeSender> emailSender = new();
        configureEmailSender(emailSender);
        ServiceCollection services = new();
        services.AddSingleton<IAuthenticationChallengeStore>(Mock.Of<IAuthenticationChallengeStore>());
        services.AddSingleton<IAuthenticationRecoveryDeliveryFailureRecorder>(Mock.Of<IAuthenticationRecoveryDeliveryFailureRecorder>());
        services.AddScoped<IEmailAuthenticationCodeSender>(_ => emailSender.Object);
        services.AddScoped<IPhoneAuthenticationCodeSender>(_ => Mock.Of<IPhoneAuthenticationCodeSender>());
        return services.BuildServiceProvider();
    }
}
