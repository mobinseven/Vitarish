using Microsoft.EntityFrameworkCore;
using Vitarish.Apps.Base;
using Vitarish.Clinic.Interface.DependencyInjection;

namespace Vitarish.Clinic.Interface;

public class ClinicApp : App, IClinicApp
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IClinicApp, ClinicApp>();
        _ = builder.Services.ConfigureMedicalInterface();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
