using Microsoft.EntityFrameworkCore;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Clinic.App;

public class ClinicApp : Vitarish.Apps.Base.Core.App
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddTransientMultiTenantDbContext<ClinicDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddClinicInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.InitializeViews();
        _ = app.MapClinicApi();

        return Task.CompletedTask;
    }
}
