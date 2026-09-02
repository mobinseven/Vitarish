namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationActiveSessionQuery
{
    Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken);
}
