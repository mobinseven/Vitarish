using Vitarish.Identity.Core.Users.Domain.Models;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <summary>
/// Interface for logging user access audit entries.
/// </summary>
public interface IUserAccessAuditLogger
{
    Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default);
}
