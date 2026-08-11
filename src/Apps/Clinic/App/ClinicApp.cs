using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Api.PatientAgent;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Clinic.App;

public class ClinicApp : Vitarish.Apps.Base.Core.App
{
    public override string Name => "Clinic";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddTransientMultiTenantDbContext<ClinicDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddClinicInfrastructure();
        _ = builder.Services.AddTransient<ClinicAgentConversationEndpoint>();
        _ = builder.Services.AddTransient<Vitarish.Intelligence.Application.Agents.IAgentConversationEndpoint>(sp => sp.GetRequiredService<ClinicAgentConversationEndpoint>());
        _ = builder.Services.AddTransient<ClinicRuntimeToolEndpoint>();
        _ = builder.Services.AddTransient<Vitarish.Intelligence.Application.Agents.IAgentRuntimeToolEndpoint>(sp => sp.GetRequiredService<ClinicRuntimeToolEndpoint>());

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        if (!DatabaseMigrationExtensions.IsOpenApiGeneration())
        {
            _ = app.InitializeViews();
        }
        _ = app.MapClinicApi();
        return Task.CompletedTask;
    }
}
