using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Files.Core.Infrastructure.Data;

namespace Vorn.Files.Core.Presentation.WebApplication;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCore(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<FilesDbContext>((sp, options) =>
        {
            dbContextOptionsBuilder(options);
        }, ServiceLifetime.Transient, ServiceLifetime.Transient);

        _ = builder.Services.AddDbContextPool<FilesDbContext>((sp, options) =>
        {
            dbContextOptionsBuilder(options);
        });

        _ = builder.Services.AddVornFilesCore();
        builder.Services.AddVornFilesCoreEntityHandlers();
        return builder;
    }
}
