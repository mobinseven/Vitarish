namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationCodeVerifier { Task<bool> VerifyAsync(string challengeId, string code, CancellationToken cancellationToken); }
