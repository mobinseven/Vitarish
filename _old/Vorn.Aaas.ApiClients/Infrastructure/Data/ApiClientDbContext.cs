using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Domain.Entities;

namespace Vorn.Aaas.ApiClients.Infrastructure.Data;

/// <summary>
/// EF Core database context for API client management.
/// </summary>
public class ApiClientDbContext(DbContextOptions<ApiClientDbContext> options) : DbContext(options)
{
    /// <summary>API client set.</summary>
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
}
