using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Vitarish.Clinic.Api;
using Vitarish.Clinic.Api.PatientAgent;
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
        _ = builder.Services.AddTransient<ClinicAgentConversationEndpoint>();
        _ = builder.Services.AddTransient<Vitarish.Intelligence.Application.Agents.IAgentConversationEndpoint>(sp => sp.GetRequiredService<ClinicAgentConversationEndpoint>());

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.InitializeViews();
        _ = app.MapClinicApi();
        return Task.CompletedTask;
    }
}
