using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationContinuationStore
{
    Task AddAsync(AuthenticationContinuation continuation, CancellationToken cancellationToken);
    Task<AuthenticationContinuation?> FindAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> TryCompleteAsync(AuthenticationContinuation continuation, DateTime now, CancellationToken cancellationToken);
}

/// <summary>Opaque host-only cookie binding a continuation to its initiating browser.</summary>
public interface IAuthenticationBrowserCorrelation
{
    string? Read(string correlationHash);
    void Write(string correlation, DateTime expiresAt);
    void Clear(string correlationHash);
}
