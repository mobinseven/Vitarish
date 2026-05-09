using Microsoft.EntityFrameworkCore;
using Vitarish.Commerce.Infrastructure.Data;
using Vitarish.Commerce.Infrastructure.DependencyInjection;
using Vitarish.Commerce.Infrastructure.Services;
using Vitarish.Commerce.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Commerce.Interface;

public class CommerceModule : Module, ICommerceModule
{
    public override string Name => "Commerce";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ICommerceModule, CommerceModule>();

        _ = builder.Services.AddDbContext<CommerceDbContext>(
            (_, options) => configureDbContext?.Invoke(options),
            ServiceLifetime.Transient);

        _ = builder.Services.AddCommerceInfrastructure();
        _ = builder.Services.ConfigureCommerceInterface();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        await app.InitializeViews();
    }
}
