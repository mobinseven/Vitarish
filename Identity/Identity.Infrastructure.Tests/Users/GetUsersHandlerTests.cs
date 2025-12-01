using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Vitarish.Identity.Core.Users.Application.Queries;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Infrastructure.Users.Data;
using Vitarish.Identity.Infrastructure.Users.Handlers;

namespace Vitarish.Identity.Infrastructure.Tests.Users;

public class GetUsersHandlerTests
{
    private readonly UsersDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly GetUsersHandler _handler;

    public GetUsersHandlerTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UsersDbContext(options);
        _cache = Substitute.For<IDistributedCache>();
        
        // Return null for cache lookups to bypass cache
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        
        _handler = new GetUsersHandler(_dbContext, _cache);
    }

    [Fact]
    public async Task Handle_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetUsers();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = new User("id-1", "user1", "user1@example.com", null, "User", "One");
        var user2 = new User("id-2", "user2", "user2@example.com", null, "User", "Two");
        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        var query = new GetUsers();

        // Act
        var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Id == "id-1");
        Assert.Contains(result, u => u.Id == "id-2");
    }
}
