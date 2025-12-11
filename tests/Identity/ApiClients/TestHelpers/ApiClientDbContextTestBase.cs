using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Infrastructure.Data;

namespace Vitarish.ApiClients.Tests.TestHelpers;

/// <summary>
/// Base class for tests that need an in-memory <see cref="ApiClientDbContext"/>.
/// Provides a shared database name so multiple DbContext instances can point to the same database.
/// </summary>
public abstract class ApiClientDbContextTestBase
{
    /// <summary>
    /// The unique database name for this test instance.
    /// Using a unique name per test class instance ensures test isolation.
    /// </summary>
    private readonly string _databaseName = Guid.NewGuid().ToString();

    /// <summary>
    /// Creates an in-memory <see cref="ApiClientDbContext"/> instance.
    /// Multiple calls will return different DbContext instances pointing to the same in-memory database,
    /// which is useful for testing persistence by verifying data with a separate context.
    /// </summary>
    protected ApiClientDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApiClientDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;
        return new ApiClientDbContext(options);
    }
}
