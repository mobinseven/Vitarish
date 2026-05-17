using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Options;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class TemporaryAutoAuthLinkCleanup(
    UsersDbContext dbContext,
    IOptions<TemporaryAutoAuthLinkOptions> options) : ITemporaryAutoAuthLinkCleanup
{
    public Task<int> CleanupAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        int retentionDays = options.Value.RetentionDays > 0
            ? options.Value.RetentionDays
            : 7;
        DateTimeOffset cutoff = now.AddDays(-retentionDays);

        return dbContext.TemporaryAutoAuthLinks
            .Where(l =>
                (l.ConsumedAt != null && l.ConsumedAt < cutoff)
                || (l.ConsumedAt == null && l.ExpiresAt < cutoff))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
