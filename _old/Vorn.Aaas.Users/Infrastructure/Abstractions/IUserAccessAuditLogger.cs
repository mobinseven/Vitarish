using Vorn.Aaas.Users.Domain.Models;

namespace Vorn.Aaas.Users.Infrastructure.Abstractions;

public interface IUserAccessAuditLogger
{
    Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default);
}
