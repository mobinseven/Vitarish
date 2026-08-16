using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Audit.Server;

public sealed class AuditDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql("Host=localhost;Database=vitarish;Username=vitarish;Password=design-time")
            .Options;
        return new AuditDbContext(options, new DesignRequestContext());
    }

    private sealed class DesignRequestContext : IRequestContext
    {
        public string Authority => string.Empty;
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => "design-time";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;

        public Request GetRequest() => new() { RequestId = "design-time", Authority = "design-time", TenantId = TenantId };
    }
}
