using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Vitarish.Appointments.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.DependencyInjection;
using Vitarish.Appointments.Infrastructure.Serializers;
using Vitarish.Appointments.Api;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Infrastructure.AgentCapabilities;

namespace Vitarish.Appointments.Module;

public class AppointmentsModule : Modules.Base.Core.Module, IAppointmentsModule
{
    public override string Name => "Appointments";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishAppointmentsDomainEntityHandlers();

        _ = builder.Services.AddSingleton<IAppointmentsModule, AppointmentsModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddTransientMultiTenantDbContext<AppointmentsDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddAppointmentsInfrastructure();
        _ = builder.Services.AddSingleton(TimeProvider.System);
        _ = builder.Services.AddTransient<IPatientAppointmentWorkflowStore, PatientAppointmentWorkflowStore>();
        _ = builder.Services.AddTransient<IPatientAppointmentAvailability, PatientAppointmentAvailability>();
        _ = builder.Services.AddTransient<CheckPatientAppointmentAvailability>();
        _ = builder.Services.AddTransient<PreparePatientAppointment>();
        _ = builder.Services.AddTransient<ConfirmPatientAppointmentProposal>();
        _ = builder.Services.AddTransient<IPatientAppointmentAgentCapability, PatientAppointmentAgentCapability>();
        _ = builder.Services.AddTransient<IAppointmentSchedulingAgentCapability, AppointmentSchedulingAgentCapability>();
        _ = builder.Services.AddTransient<IAgentSchedulingContextScope, AgentSchedulingContextScope>();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppointmentsJsonSerializerContext.Default));


        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<AppointmentsDbContext>();
        await app.InitializeViews();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapAppointmentsApi();
}
