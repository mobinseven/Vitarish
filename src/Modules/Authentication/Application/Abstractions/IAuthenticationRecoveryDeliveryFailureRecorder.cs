using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationRecoveryDeliveryFailureRecorder
{
    Task RecordAsync(PasswordRecoveryChannel channel, string failureType, CancellationToken cancellationToken);
}
