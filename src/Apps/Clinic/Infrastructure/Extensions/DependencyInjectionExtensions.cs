using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;

namespace Vitarish.Clinic.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddClinicInfrastructure(this IServiceCollection services)
        {
            _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                typeof(ClinicDbContext).Assembly,
                typeof(FindOrCreatePatientHandler).Assembly));
            _ = services.AddTransient<IPatientDirectory, SeaPatientDirectory>();
            _ = services.AddTransient<PatientViewQueryShape>();
            _ = services.AddTransient<PatientViewStatsLoader>();
            _ = services.AddSingleton<PatientAgentAuthorizationPolicy>();
            _ = services.AddSingleton<PatientAgentToolAvailabilityPolicy>();
            _ = services.AddSingleton<PatientAgentApprovalPolicy>();
            _ = services.AddTransient<IPatientAgentCapability, AppointmentsPatientAgentCapability>();
            _ = services.AddTransient<IPatientAgentCapability, PrescriptionsPatientAgentCapability>();
            _ = services.AddTransient<IPatientAgentCapability, ClinicalEventsPatientAgentCapability>();
            _ = services.AddTransient<PatientAgentCapabilityRegistry>();
            _ = services.AddTransient<PatientAgentToolDispatcher>();
            _ = services.AddTransient<PatientAgentInstructionComposer>();
            _ = services.AddTransient<PatientWorkspaceAgentDefinitionFactory>();

            return services;
        }
    }
}
