using Microsoft.EntityFrameworkCore;

public static class MigrationExtensions
{
    public static async Task<WebApplication> MigrateDatabaseAsync<TContext>(this WebApplication app) where TContext : DbContext
    {
        using IServiceScope scope = app.Services.CreateScope();
        TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
        ILogger<TContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        IEnumerable<string> pending = await db.Database.GetPendingMigrationsAsync();
        if (pending.Any())
        {
            logger.LogInformation("Applying {Count} EF Core migration(s)...", pending.Count());
        }

        await db.Database.MigrateAsync(); // creates DB and applies migrations
        return app;
    }
}
