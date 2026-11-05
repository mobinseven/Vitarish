using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Design;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Contents.Infrastructure.Data;

public sealed class ContentsDbContextFactory : IDesignTimeDbContextFactory<ContentsDbContext>
{
    private const string DesignTimeValue = "design-time";

    public ContentsDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<ContentsDbContext> options = new DbContextOptionsBuilder<ContentsDbContext>()
            .UseNpgsql((string?)null, builder => builder.MigrationsAssembly(typeof(ContentsDbContext).Assembly.FullName))
            .Options;
        return new ContentsDbContext(options, [], new DesignTimeRequestContext());
    }

    private sealed class DesignTimeRequestContext : IRequestContext
    {
        public string Authority => DesignTimeValue;
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => null;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => DesignTimeValue;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { Authority = Authority, RequestId = DesignTimeValue };
    }
}
