using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Sea.Infrastructure.Data;

public sealed class SeaDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SeaDbContext>
{
    private const string DatabaseConnectionEnvironmentVariable = "VITARISH_DB_CONNECTION";
    private const string DesignTimeTenantId = "design-time";

    public SeaDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(DatabaseConnectionEnvironmentVariable)
            ?? "Host=localhost;Database=vitarish-sea-design";
        DbContextOptions<SeaDbContext> options = new DbContextOptionsBuilder<SeaDbContext>()
            .UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(SeaDbContext).Assembly.FullName))
            .Options;
        return new SeaDbContext(options, [], new DesignTimeRequestContext(), new HttpContextAccessor());
    }

    private sealed class DesignTimeRequestContext : IRequestContext
    {
        public string Authority => DesignTimeTenantId;
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => DesignTimeTenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => DesignTimeTenantId;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId!, RequestId = RequestId! };
    }
}
