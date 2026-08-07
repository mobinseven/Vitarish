using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vitarish.Modules.Base.Core;

public static class DatabaseMigrationExtensions
{
    public static bool IsOpenApiGeneration() =>
        string.Equals(Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"), "true", StringComparison.OrdinalIgnoreCase) ||
        Environment.CommandLine.Contains("dotnet-getdocument", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "OpenApiGeneration", StringComparison.OrdinalIgnoreCase);

    public static async Task<WebApplication> MigrateDatabaseAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        if (IsOpenApiGeneration())
        {
            return app;
        }

        using IServiceScope scope = app.Services.CreateScope();
        TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
        ILogger<TContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        System.Threading.CancellationToken cancellationToken = app.Lifetime.ApplicationStopping;
        IEnumerable<string> pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        int pendingCount = pending.Count();
        if (pendingCount > 0)
        {
            logger.LogInformation("Applying {Count} EF Core migration(s)...", pendingCount);
        }
        await db.Database.MigrateAsync(cancellationToken);
        return app;
    }
}
