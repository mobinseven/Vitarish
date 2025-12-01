using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Vitarish.Identity.Core.Users.Application.Commands;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Handlers;

namespace Vitarish.Identity.Infrastructure.Tests.Users;

public class DeleteUserHandlerTests
{
    private readonly UsersDbContext _dbContext;
    private readonly IPublisher _publisher;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DeleteUserHandler> _logger;
    private readonly DeleteUserHandler _handler;

    public DeleteUserHandlerTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UsersDbContext(options);
        _publisher = Substitute.For<IPublisher>();
        _cache = Substitute.For<IDistributedCache>();
        
        // Return null for cache lookups
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        
        _logger = NullLogger<DeleteUserHandler>.Instance;
        _handler = new DeleteUserHandler(_dbContext, _publisher, _cache, _logger);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldDeleteUser()
    {
        // Arrange
        var user = new User("delete-id", "deleteuser", "delete@example.com", null, "Delete", "Me");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new DeleteUser("delete-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == "delete-id");
        Assert.Null(deletedUser);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteUser("non-existent-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldPublishEvent()
    {
        // Arrange
        var user = new User("event-delete-id", "eventuser", "event@example.com", null, "Event", "Delete");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new DeleteUser("event-delete-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _publisher.Received(1).Publish(
            Arg.Any<Vitarish.Identity.Core.Users.Domain.Events.UserDeletedEvent>(),
            Arg.Any<CancellationToken>());
    }
}
