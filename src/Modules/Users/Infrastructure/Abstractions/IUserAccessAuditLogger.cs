using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Infrastructure.Abstractions;

public interface IUserAccessAuditLogger
{
    Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default);
}
