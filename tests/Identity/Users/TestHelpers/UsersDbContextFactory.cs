using Microsoft.EntityFrameworkCore;

namespace Users.Tests.TestHelpers;

internal static class UsersDbContextFactory
{
    /// <summary>
    /// Creates an in-memory UsersDbContext with a unique database name for test isolation.
    /// Ensure the returned context is disposed (for example, with await using) to release resources.
    /// </summary>
    internal static UsersDbContext Create()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UsersDbContext(options);
    }
}
