namespace Vorn.Aaas.Users.Infrastructure.Logging;

using Microsoft.Extensions.Logging;
using Vorn.Aaas.Users.Application.Auditing;

internal sealed class UserAccessAuditLogger(ILogger<UserAccessAuditLogger> logger) : IUserAccessAuditLogger
{
    public Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User access audit {@Entry}", entry);
        return Task.CompletedTask;
    }
}
