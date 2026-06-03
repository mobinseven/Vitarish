using MediatR;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Parties.Tests;

public sealed class PartiesDbContextModelTests
{
    [Fact]
    public void TenantIndicatorPrimaryIndex_UsesPostgresFilterSyntax()
    {
        DbContextOptions<PartiesDbContext> options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using PartiesDbContext context = new(options, new NoOpPublisher());

        IIndex index = context.Model
            .FindEntityType(typeof(TenantIndicator))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual(
            [
                nameof(TenantIndicator.TenantId),
                nameof(TenantIndicator.IsPrimary)
            ]));

        Assert.True(index.IsUnique);
        Assert.Equal("\"IsPrimary\" = TRUE", index.GetFilter());
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }
}
