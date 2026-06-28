using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationCodeSender { Task SendAsync(AuthenticationChallengePurpose purpose, string destination, string code, CancellationToken cancellationToken); }
