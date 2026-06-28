using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IPhoneAuthenticationCodeSender
{
    Task SendAsync(string phoneNumber, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken);
}
