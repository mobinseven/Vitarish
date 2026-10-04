using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class PasswordRecoveryCodeDispatcher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PasswordRecoveryCodeDispatcher> logger) : IPasswordRecoveryCodeDispatcher
{
    public Task DispatchAsync(AuthenticationChallenge challenge, string code, CancellationToken cancellationToken)
    {
        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(() => DeliverAsync(challenge, code));
        }

        return Task.CompletedTask;
    }

    private async Task DeliverAsync(AuthenticationChallenge challenge, string code)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IServiceProvider services = scope.ServiceProvider;
            IAuthenticationChallengeStore challengeStore = services.GetRequiredService<IAuthenticationChallengeStore>();
            IAuthenticationRecoveryDeliveryFailureRecorder failureRecorder = services.GetRequiredService<IAuthenticationRecoveryDeliveryFailureRecorder>();
            using CancellationTokenSource deliveryCancellation = new();
            Task? delivery = null;
            try
            {
                delivery = SendAsync(services, challenge, code, deliveryCancellation.Token);
                await delivery.WaitAsync(AuthenticationChallengePolicy.PasswordRecoveryDeliveryTimeout, timeProvider, CancellationToken.None);
            }
            catch (Exception exception)
            {
                deliveryCancellation.Cancel();
                challenge.Cancel();
                await challengeStore.ReplaceAsync(challenge, CancellationToken.None);
                await failureRecorder.RecordAsync(ChannelFor(challenge.Purpose), exception.GetType().Name, CancellationToken.None);
            }
            finally
            {
                if (delivery is { IsCompletedSuccessfully: false })
                {
                    _ = delivery.ContinueWith(
                        static task => _ = task.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError("Password recovery code delivery failed with {FailureType}.", exception.GetType().Name);
        }
    }

    private static Task SendAsync(IServiceProvider services, AuthenticationChallenge challenge, string code, CancellationToken cancellationToken) =>
        challenge.Purpose switch
        {
            AuthenticationChallengePurpose.EmailPasswordRecovery => services.GetRequiredService<IEmailAuthenticationCodeSender>()
                .SendAsync(challenge.Destination, code, challenge.Purpose, cancellationToken),
            AuthenticationChallengePurpose.PhonePasswordRecovery => services.GetRequiredService<IPhoneAuthenticationCodeSender>()
                .SendAsync(challenge.Destination, code, challenge.Purpose, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(challenge), "Only password recovery challenges can be dispatched.")
        };

    private static PasswordRecoveryChannel ChannelFor(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.EmailPasswordRecovery => PasswordRecoveryChannel.Email,
        AuthenticationChallengePurpose.PhonePasswordRecovery => PasswordRecoveryChannel.Sms,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose))
    };
}
