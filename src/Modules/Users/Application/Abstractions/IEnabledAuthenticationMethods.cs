namespace Vitarish.Users.Application.Abstractions;

/// <summary>Reads the effective authentication policy without exposing credentials.</summary>
public interface IEnabledAuthenticationMethods
{
    Task<EnabledAuthenticationMethods> GetAsync(CancellationToken cancellationToken);
}

public sealed record EnabledAuthenticationMethods(bool Password, bool Phone, bool Email, bool Passkey);
