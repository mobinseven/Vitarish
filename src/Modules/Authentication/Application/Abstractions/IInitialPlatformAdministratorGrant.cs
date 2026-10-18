namespace Vitarish.Authentication.Application.Abstractions;

/// <summary>
/// Gives a deployment with no platform administrator its first one: the account
/// that next verifies a contact sign-in code. Once any platform administrator
/// exists this does nothing, and further administrators are assigned in the app.
/// </summary>
public interface IInitialPlatformAdministratorGrant
{
    Task GrantIfUnclaimedAsync(string userId, CancellationToken cancellationToken);
}
