using Microsoft.EntityFrameworkCore; using Vitarish.Apps.Base; using Vitarish.Clinic.Api; using Vitarish.Clinic.Infrastructure.Data; using Vitarish.Clinic.Infrastructure.Extensions; using Vitarish.Clinic.Interface; using Vitarish.Clinic.Interface.DependencyInjection; using Vitarish.Entities.Server.Extensions;
namespace Vitarish.Clinic.Module;
public class ClinicApp : App, IClinicApp
{
    public override string Name => "Clinic";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null){ _=builder.Services.AddTransientMultiTenantDbContext<ClinicDbContext>((_,options)=>configureDbContext?.Invoke(options)); _=builder.Services.AddClinicInfrastructure(); _=builder.Services.AddSingleton<IClinicApp, ClinicApp>(); _=builder.Services.ConfigureMedicalInterface(); return Task.CompletedTask; }
    public override Task Initialize(WebApplication app){ _=app.InitializeViews(); _=app.MapClinicApi(); return Task.CompletedTask; }
}
