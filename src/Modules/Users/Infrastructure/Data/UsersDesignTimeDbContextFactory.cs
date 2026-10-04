using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vitarish.Users.Infrastructure.Data;

public sealed class UsersDesignTimeDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    private const string DatabaseConnectionEnvironmentVariable = "VITARISH_DB_CONNECTION";

    public UsersDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(DatabaseConnectionEnvironmentVariable)
            ?? "Host=localhost;Database=vitarish-users-design";
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName))
            .Options;
        return new UsersDbContext(options);
    }
}
