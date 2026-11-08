namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>Resolves registered browser origins and their current access authority.</summary>
public interface IAuthenticationHostDirectory
{
    Task<bool> IsCentralHostAsync(CancellationToken cancellationToken);
    Task<bool> CanOpenAsync(string origin, string? userId, CancellationToken cancellationToken);
    string? SignInOrigin { get; }
    string CurrentOrigin { get; }
}
