namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>
/// Whether the request's tenant lets a user with no role start a session. Normally a session
/// needs a role; platform hosts whose tenant holds no practice data are the exception (the
/// sign-up, sign-in and landing hosts), so a new account can act there before it has a clinic.
/// Such a session has no tenant access, so every tenant endpoint still refuses it. Several
/// owners may register a policy; a session is admitted when any of them admits it.
/// </summary>
public interface IRoleFreeSessionPolicy
{
    Task<bool> AllowsRoleFreeSessionAsync(CancellationToken cancellationToken = default);
}
