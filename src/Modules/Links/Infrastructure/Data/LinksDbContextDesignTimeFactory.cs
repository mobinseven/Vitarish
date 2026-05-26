using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vitarish.Links.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations) when the full
/// application host cannot be started (e.g. missing environment configuration).
/// </summary>
internal sealed class LinksDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LinksDbContext>
{
    public LinksDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<LinksDbContext> optionsBuilder = new();
        _ = optionsBuilder.UseNpgsql(
            "Host=localhost;Database=Vitarish_Design;Username=postgres;******;",
            b => b.MigrationsAssembly("Vitarish.Main"));

        return new LinksDbContext(optionsBuilder.Options, Enumerable.Empty<IInterceptor>());
    }
}
