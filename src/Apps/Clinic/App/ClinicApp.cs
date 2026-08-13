using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Api.ClinicAssistant;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Extensions;
using Vitarish.Clinic.Agents;
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
        _ = builder.Services.AddSingleton(new ClinicAssistantOptions(
            builder.Configuration["Vitarish:Intelligence:DefaultResponseModel"]
            ?? throw new InvalidOperationException("Vitarish:Intelligence:DefaultResponseModel must be configured.")));
        _ = builder.Services.AddClinicAssistantComposition();
        _ = builder.Services.AddTransient<ClinicAssistantConversationEndpoint>();
        _ = builder.Services.AddTransient<Vitarish.Intelligence.Application.Agents.IAgentConversationEndpoint>(sp => sp.GetRequiredService<ClinicAssistantConversationEndpoint>());
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
