using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Estates.Base;
using Vitarish.Sea.Interface;

public static class EstatesExtensions
{
    public static List<IEstate> GetEstates() =>
            [
                Activator.CreateInstance<SeaEstate>(),
                //Activator.CreateInstance<CmsEstate>(),
            ];
    public static async Task<WebApplicationBuilder> RegisterEstates(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        List<IEstate> estates = GetEstates();

        builder.Services.TryAddSingleton<IReadOnlyList<IEstate>>(estates);

        foreach (IEstate estate in estates)
        {
            await estate.Register(builder, configureDbContext);
        }

        return builder;
    }
    public static async Task<WebApplication> InitializeEstates(this WebApplication app)
    {
        IReadOnlyList<IEstate>? estates = app.Services.GetService<IReadOnlyList<IEstate>>();
        if (estates is null)
        {
            return app;
        }

        foreach (IEstate estate in estates)
        {
            await estate.Initialize(app);
        }

        return app;
    }
}
