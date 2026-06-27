using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Modules.Base.Interface.Abstractions;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Infrastructure.Data;
using Vitarish.Platform.Infrastructure.DependencyInjection;
using Vitarish.Platform.Infrastructure.Services;
using Vitarish.Platform.Module.Services;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Platform.Module;

public sealed class PlatformModule : Modules.Base.Core.Module
{
    private const int ThemeSelectionId = 1;
    public override string Name => "Platform";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddPlatform(configureDbContext ?? (_ => { }));
        builder.Services.TryAddTransient<IPlatformThemePaletteProvider, PlatformThemePaletteProvider>();
        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<PlatformDbContext>();

        using IServiceScope scope = app.Services.CreateScope();
        PlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        TimeProvider timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        DateTimeOffset now = timeProvider.GetUtcNow();

        bool hasDefault = await dbContext.Themes.AnyAsync(e => e.Id == DefaultPlatformTheme.Id);
        if (!hasDefault)
        {
            _ = dbContext.Themes.Add(DefaultPlatformTheme.Create());
        }

        ThemeSelection? selection = await dbContext.ThemeSelections.SingleOrDefaultAsync(e => e.Id == ThemeSelectionId);
        if (selection is null)
        {
            _ = dbContext.ThemeSelections.Add(new ThemeSelection
            {
                Id = ThemeSelectionId,
                ActiveThemeId = DefaultPlatformTheme.Id,
                UpdatedAt = now
            });
        }

        _ = await dbContext.SaveChangesAsync();
    }
}
