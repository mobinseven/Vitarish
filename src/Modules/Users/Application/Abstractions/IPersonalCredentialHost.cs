namespace Vitarish.Users.Application.Abstractions;

/// <summary>Identifies the host that owns personal credential management.</summary>
public interface IPersonalCredentialHost
{
    Task<bool> AllowsManagementAsync(CancellationToken cancellationToken);
}
