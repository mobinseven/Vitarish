using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationDbContextFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
{
    public AuthenticationDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("VITARISH_DB_CONNECTION")
            ?? throw new InvalidOperationException("VITARISH_DB_CONNECTION must be set for Authentication database tooling.");
        DbContextOptionsBuilder<AuthenticationDbContext> options = new();
        _ = options.UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(AuthenticationDbContext).Assembly.FullName));
        return new AuthenticationDbContext(options.Options);
    }
}
