using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Infrastructure.Services;

namespace Vitarish.Main.Infrastructure.DesignTime;

public sealed class ClinicalDbContextFactory : IDesignTimeDbContextFactory<ClinicalDbContext>
{
    public ClinicalDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Db")
            ?? "Server=localhost;Database=vitarish;User Id=sa;Password=Password123!;TrustServerCertificate=True;";

        DbContextOptionsBuilder<ClinicalDbContext> optionsBuilder = new();
        _ = optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Vitarish.Main"));

        HttpContextAccessor httpContextAccessor = new();
        RequestContext requestContext = new(httpContextAccessor);

        return new ClinicalDbContext(optionsBuilder.Options, Array.Empty<IInterceptor>(), requestContext, httpContextAccessor);
    }
}
