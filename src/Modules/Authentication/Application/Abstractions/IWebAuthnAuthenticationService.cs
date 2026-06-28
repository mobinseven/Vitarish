namespace Vitarish.Authentication.Application.Abstractions;

public interface IWebAuthnAuthenticationService { Task<string> BeginAsync(string? login, CancellationToken cancellationToken); Task<string?> CompleteAsync(string challengeId, string credentialJson, CancellationToken cancellationToken); }
