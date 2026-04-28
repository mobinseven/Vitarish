using Microsoft.EntityFrameworkCore;
using Vitarish.Medical.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Medical.Interface;

public class MedicalModule : Module, IMedicalModule
{
    public override string Name => "Medical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IMedicalModule, MedicalModule>();
        _ = builder.Services.ConfigureMedicalInterface();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
