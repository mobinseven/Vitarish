using Microsoft.EntityFrameworkCore;
using Vitarish.Apps.Base;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Clinic.Interface.DependencyInjection;
using Vitarish.Entities.Server.Extensions;
namespace Vitarish.Clinic.Interface;

public class ClinicApp : App, IClinicApp
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddTransientMultiTenantDbContext<ClinicDbContext>((_, options) => configureDbContext(options));
        _ = builder.Services.AddClinicInfrastructure();
        _ = builder.Services.AddSingleton<IClinicApp, ClinicApp>();
        _ = builder.Services.ConfigureMedicalInterface();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => app.InitializeViews();
}
