using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Data;

/// <summary>
/// EF Core database context for authentication client management.
/// </summary>
public class AuthenticationClientDbContext(DbContextOptions<AuthenticationClientDbContext> options) : DbContext(options)
{
    /// <summary>Authentication client set.</summary>
    public DbSet<AuthenticationClient> AuthenticationClients => Set<AuthenticationClient>();
}
