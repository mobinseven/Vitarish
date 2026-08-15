using Vitarish.Medical.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Medical.Infrastructure.Data;
using Vitarish.Medical.Infrastructure.DependencyInjection;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Medical.Api;
using Vitarish.Modules.Base.Core;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;
using Vitarish.Medical.Infrastructure.AgentCapabilities.Prescriptions;

namespace Vitarish.Medical.Module;

public sealed class MedicalModule : Modules.Base.Core.Module, IMedicalModule
{
    public override string Name => "Medical";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<IMedicalModule, MedicalModule>();
        _ = builder.Services.AddTransientDbContext<MedicalDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddTransientMultiTenantDbContext<PrescriptionDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = builder.Services.AddMedicalInfrastructure(builder.Configuration);
        _ = builder.Services.AddTransient<SearchPrescriptionTemplatesForAgent>();
        _ = builder.Services.AddTransient<GetPrescriptionTemplateRequirements>();
        _ = builder.Services.AddTransient<PreparePrescriptionDraftFromTemplate>();
        _ = builder.Services.AddTransient<ConfirmPrescriptionDraft>();
        _ = builder.Services.AddSingleton<IPrescriptionAgentReviewStore, InMemoryPrescriptionAgentReviewStore>();
        _ = builder.Services.AddTransient<IPrescriptionAgentCapability, PrescriptionAgentCapability>();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<MedicalDbContext>();
        _ = await app.MigrateDatabaseAsync<PrescriptionDbContext>();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapMedicalApi();
}
