using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Clinical.Infrastructure.DependencyInjection;
using Vitarish.Clinical.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Clinical.Interface;

public sealed class ClinicalModule : Module, IClinicalModule
{
    public override string Name => "Clinical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IClinicalModule, ClinicalModule>();
        _ = builder.Services.AddDbContext<ClinicalDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);
        _ = builder.Services.AddClinicalInfrastructure();
        _ = builder.Services.ConfigureClinicalInterface();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
