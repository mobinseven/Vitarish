using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IEmailAuthenticationCodeSender
{
    Task SendAsync(string email, string code, AuthenticationChallengePurpose purpose, CancellationToken cancellationToken);
}
