using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Files.Infrastructure.Data;

public sealed class FilesDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FilesDbContext>
{
    private const string DatabaseConnectionEnvironmentVariable = "VITARISH_DB_CONNECTION";

    public FilesDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(DatabaseConnectionEnvironmentVariable)
            ?? "Host=localhost;Database=vitarish-files-design";
        DbContextOptions<FilesDbContext> options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseNpgsql(connectionString, builder => builder.MigrationsAssembly(typeof(FilesDbContext).Assembly.FullName))
            .Options;
        return new FilesDbContext(options, [], new DesignTimeRequestContext(), new HttpContextAccessor());
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
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId, RequestId = "design-time" };
    }
}
