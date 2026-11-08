namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>Returns through Auth after clinic creation has ended the old browser session.</summary>
public interface IAuthenticationCreationHandoff
{
    Task<string?> CreateAsync(string userId, string clinicHostname, CancellationToken cancellationToken);
}

public interface IAuthenticationSessionRootReader
{
    Task<Guid?> FindAsync(string sessionId, string userId, CancellationToken cancellationToken);
}

/// <summary>Reads only a cryptographically verified, unexpired prior Auth cookie; grants no session authority.</summary>
public interface IAuthenticationPreviousSessionCookie
{
    Guid? Read(string userId);
}
