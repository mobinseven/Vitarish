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

        IQueryable<Domain.Entities.TemporaryAutoAuthLink> query = dbContext.TemporaryAutoAuthLinks
            .Where(l =>
                (l.ConsumedAt != null && l.ConsumedAt < cutoff)
                || (l.ConsumedAt == null && l.ExpiresAt < cutoff));

        if (dbContext.Database.IsRelational())
        {
            return query.ExecuteDeleteAsync(cancellationToken);
        }

        return CleanupWithInMemoryFallbackAsync(query, cancellationToken);
    }

    private async Task<int> CleanupWithInMemoryFallbackAsync(
        IQueryable<Domain.Entities.TemporaryAutoAuthLink> query,
        CancellationToken cancellationToken)
    {
        List<Domain.Entities.TemporaryAutoAuthLink> links = await query.ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return 0;
        }

        dbContext.TemporaryAutoAuthLinks.RemoveRange(links);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return links.Count;
    }
}
