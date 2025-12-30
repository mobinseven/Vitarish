namespace Vitarish.Parties.Tests;

using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Events;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Handlers;
using Vitarish.Parties.Tests.TestHelpers;

public class OwnerCommandHandlerTests : IDisposable
{
    private readonly PartiesDbContext _dbContext;
    private readonly RecordingPublisher _publisher;

    public OwnerCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PartiesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _publisher = new RecordingPublisher();
        _dbContext = new PartiesDbContext(options, _publisher);
    }

    [Fact]
    public async Task CreateOwner_PublishesEventAndPersistsOwner()
    {
        var handler = new CreateOwnerHandler(_dbContext, _publisher);
        var command = new CreateOwner("Owner", "logo", "desc", "creator");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Owner", result.Name);
        Assert.Equal("logo", result.LogoUrl);
        Assert.Equal("desc", result.Description);
        Assert.Equal("creator", result.CreatedBy);
        Assert.Contains(_publisher.Notifications, notification =>
            notification is OwnerCreatedEvent createdEvent && createdEvent.OwnerId == result.Id);
    }

    [Fact]
    public async Task UpdateOwner_ReturnsNullWhenMissing()
    {
        var handler = new UpdateOwnerHandler(_dbContext);
        var command = new UpdateOwner(Guid.NewGuid(), "Owner");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOwner_UpdatesFields()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateOwnerHandler(_dbContext);
        var command = new UpdateOwner(owner.Id, "Updated", "logo", "desc", "modifier");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal("logo", result.LogoUrl);
        Assert.Equal("desc", result.Description);
        Assert.Equal("modifier", result.ModifiedBy);
        Assert.NotNull(result.ModifiedAt);
    }

    [Fact]
    public async Task DeleteOwner_RemovesOwner()
    {
        var owner = new Owner
        {
            Id = Guid.NewGuid(),
            Name = "Owner"
        };
        _dbContext.Owners.Add(owner);
        await _dbContext.SaveChangesAsync();

        var handler = new DeleteOwnerHandler(_dbContext);
        var command = new DeleteOwner(owner.Id);

        await handler.Handle(command, CancellationToken.None);

        Owner? deleted = await _dbContext.Owners.FindAsync(owner.Id);
        Assert.Null(deleted);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
