using Microsoft.Extensions.Logging;
using Vitarish.Identity.Core.Users.Domain.Models;

namespace Vitarish.Identity.Infrastructure.Users.Services;

/// <inheritdoc />
internal sealed class UserAccessAuditLogger(ILogger<UserAccessAuditLogger> logger) : IUserAccessAuditLogger
{
    public Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User access audit {@Entry}", entry);
        return Task.CompletedTask;
    }
}
