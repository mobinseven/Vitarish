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

public class CreateUserHandlerTests
{
    private readonly UsersDbContext _dbContext;
    private readonly IPublisher _publisher;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UsersDbContext(options);
        _publisher = Substitute.For<IPublisher>();
        _cache = Substitute.For<IDistributedCache>();
        
        // Return null for cache lookups
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        
        _logger = NullLogger<CreateUserHandler>.Instance;
        _handler = new CreateUserHandler(_dbContext, _publisher, _cache, _logger);
    }

    [Fact]
    public async Task Handle_CreateUserWithEmail_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateUserWithEmail("test@example.com", "John", "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("test@example.com", result.Value.Email);
        Assert.Equal("John", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal("test@example.com", result.Value.UserName);

        // Verify user was persisted
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.Value.Id);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task Handle_CreateUserWithPhone_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateUserWithPhone("+1234567890", "Jane", "Smith");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("+1234567890", result.Value.PhoneNumber);
        Assert.Equal("Jane", result.Value.FirstName);
        Assert.Equal("Smith", result.Value.LastName);
        Assert.Equal("+1234567890", result.Value.UserName);

        // Verify user was persisted
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.Value.Id);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task Handle_CreateUserWithEmail_ShouldPublishEvent()
    {
        // Arrange
        var command = new CreateUserWithEmail("event@example.com", "Event", "Test");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        await _publisher.Received(1).Publish(
            Arg.Any<Vitarish.Identity.Core.Users.Domain.Events.UserCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }
}
