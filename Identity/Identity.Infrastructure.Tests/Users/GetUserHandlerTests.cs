using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Handlers;

namespace Vitarish.Identity.Infrastructure.Tests.Users;

public class GetUserHandlerTests
{
    private readonly UsersDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UsersDbContext(options);
        _cache = Substitute.For<IDistributedCache>();
        
        // Return null for cache lookups to bypass cache
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        
        _handler = new GetUserHandler(_dbContext, _cache);
    }

    [Fact]
    public async Task Handle_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = new User("test-id", "testuser", "test@example.com", null, "Test", "User");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var query = new GetUser("test-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    [Fact]
    public async Task Handle_NonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var query = new GetUser("non-existent-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
