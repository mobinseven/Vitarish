using Microsoft.EntityFrameworkCore;

public static class MigrationExtensions
{
    public static async Task<WebApplication> MigrateDatabaseAsync<TContext>(this WebApplication app) where TContext : DbContext
    {
        using IServiceScope scope = app.Services.CreateScope();
        TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
        ILogger<TContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        // For SQLite: make sure the directory exists (EF/SQLite won't create it)
        //if (db.Database.GetDbConnection() is SqliteConnection sqlite)
        //{
        //    string dataSource = sqlite.DataSource; // may be relative
        //    string fullPath = Path.IsPathRooted(dataSource)
        //        ? dataSource
        //        : Path.Combine(AppContext.BaseDirectory, dataSource);

        //    string? dir = Path.GetDirectoryName(fullPath);
        //    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        //    {
        //        Directory.CreateDirectory(dir);
        //    }
        //}

        IEnumerable<string> pending = await db.Database.GetPendingMigrationsAsync();
        if(pending.Any())
        {
            logger.LogInformation("Applying {Count} EF Core migration(s)...", pending.Count());
        }

        await db.Database.MigrateAsync(); // creates DB and applies migrations
        return app;
    }
}
