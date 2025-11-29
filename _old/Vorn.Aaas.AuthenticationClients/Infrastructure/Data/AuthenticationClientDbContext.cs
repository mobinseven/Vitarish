using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

namespace Vorn.Aaas.AuthenticationClients.Infrastructure.Data;

/// <summary>
/// EF Core database context for authentication client management.
/// </summary>
public class AuthenticationClientDbContext(DbContextOptions<AuthenticationClientDbContext> options) : DbContext(options)
{
    /// <summary>Authentication client set.</summary>
    public DbSet<AuthenticationClient> AuthenticationClients => Set<AuthenticationClient>();
}
