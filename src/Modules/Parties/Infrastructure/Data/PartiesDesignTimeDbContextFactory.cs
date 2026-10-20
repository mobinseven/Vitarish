using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vitarish.Parties.Infrastructure.Data;

public sealed class PartiesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PartiesDbContext>
{
    private const string DatabaseConnectionEnvironmentVariable = "VITARISH_DB_CONNECTION";

    public PartiesDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(DatabaseConnectionEnvironmentVariable)
            ?? "Host=localhost;Database=vitarish-parties-design";
        DbContextOptions<PartiesDbContext> options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(PartiesDbContext).Assembly.FullName))
            .Options;
        return new PartiesDbContext(options, new DesignTimePublisher(), []);
    }

    // Design-time tools never save changes, so no domain event is ever published.
    private sealed class DesignTimePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
