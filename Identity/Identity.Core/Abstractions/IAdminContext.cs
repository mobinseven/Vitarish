namespace Vitarish.Identity.Core.Abstractions;

/// <summary>
/// Provides administrative context information for identity operations.
/// This is used for operations that require admin-level access.
/// </summary>
public interface IAdminContext
{
    /// <summary>
    /// Gets the current admin user's identifier.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the owner identifier for administrative operations.
    /// </summary>
    string? OwnerId { get; }

    /// <summary>
    /// Indicates whether the current user has admin privileges.
    /// </summary>
    bool IsAdmin { get; }
}
