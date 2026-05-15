using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Vitarish.Contents.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Main.Migrations.ContentsDb;

public sealed class DesignTimeContentsDbContextFactory : IDesignTimeDbContextFactory<ContentsDbContext>
{
    public ContentsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ContentsDbContext> optionsBuilder = new();
        _ = optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=VitarishDesignTimeContents;Trusted_Connection=True;TrustServerCertificate=True;",
            builder => builder.MigrationsAssembly("Vitarish.Main"));

        return new ContentsDbContext(
            optionsBuilder.Options,
            Array.Empty<IInterceptor>(),
            new DesignTimeRequestContext());
    }

    private sealed class DesignTimeRequestContext : IRequestContext
    {
        public string Authority => "design-time";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => "design-time";
        public string? TenantId => "design-time";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;

        public Request GetRequest() => new()
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Authority = Authority,
            UserId = UserId,
            TenantId = TenantId
        };
    }
}
