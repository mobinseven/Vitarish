namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>
/// Whether the request's tenant lets a user with no role start a session. Normally a session
/// needs a role; the sign-up host is the exception, so a new account can complete its profile
/// and create its own tenant there. Such a session has no tenant access, so every tenant
/// endpoint still refuses it.
/// </summary>
public interface IRoleFreeSessionPolicy
{
    Task<bool> AllowsRoleFreeSessionAsync(CancellationToken cancellationToken = default);
}
