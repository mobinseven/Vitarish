namespace Vitarish.Contents.Infrastructure.Data;

internal static class ContentPublicationLocks
{
    internal static Task AcquireAsync(
        ContentsDbContext dbContext,
        string tenantId,
        Guid resourceId,
        CancellationToken cancellationToken) => dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({tenantId}), hashtext({resourceId.ToString("N")}))",
            cancellationToken);
}
