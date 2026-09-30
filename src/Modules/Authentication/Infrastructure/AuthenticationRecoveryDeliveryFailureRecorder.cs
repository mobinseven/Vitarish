using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationRecoveryDeliveryFailureRecorder(
    ILogger<AuthenticationRecoveryDeliveryFailureRecorder> logger) : IAuthenticationRecoveryDeliveryFailureRecorder
{
    public Task RecordAsync(PasswordRecoveryChannel channel, string failureType, CancellationToken cancellationToken)
    {
        logger.LogError("Password recovery code delivery failed for {RecoveryChannel} with {FailureType}.", channel, failureType);
        return Task.CompletedTask;
    }
}
