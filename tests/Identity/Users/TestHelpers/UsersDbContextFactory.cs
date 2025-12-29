using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Tests.TestHelpers;

internal static class UsersDbContextFactory
{
    internal static UsersDbContext Create()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UsersDbContext(options);
    }
}
