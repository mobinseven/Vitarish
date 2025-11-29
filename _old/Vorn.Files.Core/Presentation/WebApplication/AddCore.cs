using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Vorn.Files.Core.Infrastructure.Data;

namespace Vorn.Files.Core.Presentation.WebApplication;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCore(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddDbContext<FilesDbContext>((sp, options) =>
        {
            dbContextOptionsBuilder(options);
        }, ServiceLifetime.Transient, ServiceLifetime.Transient);
        builder.Services.AddDbContextPool<FilesDbContext>((sp, options) =>
        {
            string exeFullName = Assembly.GetEntryAssembly()!.Location;
            string dirFullName = Path.GetDirectoryName(exeFullName)!;
            string assembly = Assembly.GetCallingAssembly().GetName().Name;
            string dbPath = Path.Combine(dirFullName, $"{assembly}.db");
            options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly(assembly));
            options.AddInterceptors(sp.GetRequiredService<EntityManagementInterceptor>());
        });
        builder.Services.AddVornFilesCore();
        builder.Services.AddVornFilesCoreEntityHandlers();
        return builder;
    }
}
