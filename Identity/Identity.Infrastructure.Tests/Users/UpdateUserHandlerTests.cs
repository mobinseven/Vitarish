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

public class UpdateUserHandlerTests
{
    private readonly UsersDbContext _dbContext;
    private readonly IPublisher _publisher;
    private readonly IDistributedCache _cache;
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UsersDbContext(options);
        _publisher = Substitute.For<IPublisher>();
        _cache = Substitute.For<IDistributedCache>();
        
        // Return null for cache lookups
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        
        _logger = NullLogger<UpdateUserHandler>.Instance;
        _handler = new UpdateUserHandler(_dbContext, _publisher, _cache, _logger);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldUpdateUser()
    {
        // Arrange
        var user = new User("update-id", "updateuser", "update@example.com", null, "Original", "Name");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var command = new UpdateUser("update-id", "Updated", "UserName");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == "update-id");
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated", updatedUser.FirstName);
        Assert.Equal("UserName", updatedUser.LastName);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateUser("non-existent-id", "New", "Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }
}
