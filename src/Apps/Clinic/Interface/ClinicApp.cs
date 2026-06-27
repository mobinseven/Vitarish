using Vitarish.Apps.Base;
using Vitarish.Clinic.Interface.DependencyInjection;
namespace Vitarish.Clinic.Interface;

public class ClinicApp : App, IClinicApp
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IClinicApp, ClinicApp>();
        _ = builder.Services.ConfigureMedicalInterface();
        return Task.CompletedTask;
    }
}
