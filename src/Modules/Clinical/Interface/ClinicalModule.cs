using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Clinical.Interface;

public class ClinicalModule : Module, IClinicalModule
{
    public override string Name => "Clinical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IClinicalModule, ClinicalModule>();
        _ = builder.Services.ConfigureMedicalInterface();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
