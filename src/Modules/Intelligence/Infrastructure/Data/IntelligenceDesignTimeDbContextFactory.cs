using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Security.Claims;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Intelligence.Infrastructure.Data;

public sealed class IntelligenceDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IntelligenceDbContext>
{
    public IntelligenceDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<IntelligenceDbContext> options = new DbContextOptionsBuilder<IntelligenceDbContext>()
            .UseNpgsql("Host=localhost;Database=vitarish;Username=vitarish;Password=design-time")
            .Options;
        return new IntelligenceDbContext(options, [], new DesignRequestContext(), new HttpContextAccessor());
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
