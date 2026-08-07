using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Design;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Communications.Infrastructure.Data;

public sealed class CommunicationsDbContextFactory : IDesignTimeDbContextFactory<CommunicationsDbContext>
{
    public CommunicationsDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("VITARISH_DB_CONNECTION")
            ?? "Host=localhost;Database=vitarish;Username=postgres;Password=postgres";
        DbContextOptionsBuilder<CommunicationsDbContext> options = new();
        _ = options.UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(CommunicationsDbContext).Assembly.FullName));
        return new CommunicationsDbContext(options.Options, [], new DesignTimeRequestContext());
    }

    private sealed class DesignTimeRequestContext : IRequestContext
    {
        public string Authority => "design-time";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => "design-time";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "design-time";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId, RequestId = RequestId };
    }
}
