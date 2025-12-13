namespace Vitarish.Sea.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Presentation.Serializers;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddSeaCore(
        this WebApplicationBuilder builder,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(dbContextOptionsBuilder);

        _ = builder.Services.AddDbContext<SeaDbContext>((sp, options) =>
        {
            dbContextOptionsBuilder(sp, options);
        }, ServiceLifetime.Transient, ServiceLifetime.Transient);
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddVitarishSeaInfrastructure();
        _ = builder.Services.AddAppMediatR(typeof(WebApplicationBuilderExtensions).Assembly);
        _ = builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));
        return builder;
    }
}
