using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Infrastructure.Data;
using Vitarish.Platform.Infrastructure.Services;

namespace Vitarish.Platform.Infrastructure.DependencyInjection;

public static class PlatformDependencyInjectionExtensions
{
    public static void AddPlatform(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> configureDbContext)
    {
        _ = builder.Services.AddTransientDbContext<PlatformDbContext>((_, options) => configureDbContext(options));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddTransient<PlatformThemeStore>();
        builder.Services.TryAddTransient<IPlatformThemeQuery>(sp => sp.GetRequiredService<PlatformThemeStore>());
        builder.Services.TryAddTransient<IPlatformThemeCommand>(sp => sp.GetRequiredService<PlatformThemeStore>());
    }
}
