
using Microsoft.Extensions.Logging;
using Vorn.Aaas.Users.Domain.Models;
using Vorn.Aaas.Users.Infrastructure.Abstractions;

namespace Vorn.Aaas.Users.Infrastructure.Service;

internal sealed class UserAccessAuditLogger(ILogger<UserAccessAuditLogger> logger) : IUserAccessAuditLogger
{
    public Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User access audit {@Entry}", entry);
        return Task.CompletedTask;
    }
}
