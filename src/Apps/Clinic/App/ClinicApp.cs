using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Apps.Base.Core;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Clinic.Interface;
using Vitarish.Clinic.Interface.DependencyInjection;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Clinic.App;

public class ClinicApp : Vitarish.Apps.Base.Core.App, IClinicApp
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddTransientMultiTenantDbContext<ClinicDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddClinicInfrastructure();
        _ = builder.Services.AddSingleton<IClinicApp, ClinicApp>();
        _ = builder.Services.ConfigureMedicalInterface();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.InitializeViews();
        _ = app.MapClinicApi();

        return Task.CompletedTask;
    }
}

public interface IClinicApp : IApp;
