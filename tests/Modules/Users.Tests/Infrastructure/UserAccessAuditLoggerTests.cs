using Users.Tests.TestHelpers;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Service;

namespace Users.Tests.Infrastructure;

public class UserAccessAuditLoggerTests
{
    [Fact]
    public async Task LogAsync_WritesLogEntry()
    {
        TestLogger<UserAccessAuditLogger> logger = new();
        UserAccessAuditLogger auditLogger = new(logger);
        UserAccessAuditEntry entry = new(DateTimeOffset.UtcNow, "admin-1", "owner-1", "user-1", "client-1", "access");

        await auditLogger.LogAsync(entry, CancellationToken.None);

        _ = logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("User access audit"));
    }
}
