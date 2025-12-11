
using Microsoft.Extensions.Logging;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Abstractions;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class UserAccessAuditLogger(ILogger<UserAccessAuditLogger> logger) : IUserAccessAuditLogger
{
    public Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User access audit {@Entry}", entry);
        return Task.CompletedTask;
    }
}
