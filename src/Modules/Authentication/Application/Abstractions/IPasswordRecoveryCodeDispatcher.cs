using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IPasswordRecoveryCodeDispatcher
{
    Task DispatchAsync(AuthenticationChallenge challenge, string code, CancellationToken cancellationToken);
}
