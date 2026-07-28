using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Application.PatientAgent.Capabilities.Appointments;
using Vitarish.Clinic.Application.PatientAgent.Capabilities.Prescriptions;
using Vitarish.Clinic.Application.PatientAgent.Capabilities.ClinicalEvents;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.ClinicalEvents;

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
            _ = services.AddTransient<CheckAppointmentAvailabilityTool>();
            _ = services.AddTransient<PrepareAppointmentTool>();
            _ = services.AddTransient<ConfirmAppointmentTool>();
            _ = services.AddTransient<IPatientAgentCapability, AppointmentAgentCapability>();
            _ = services.AddTransient<SearchPrescriptionTemplatesTool>();
            _ = services.AddTransient<GetPrescriptionTemplateRequirementsTool>();
            _ = services.AddTransient<PreparePrescriptionTool>();
            _ = services.AddTransient<ConfirmPrescriptionTool>();
            _ = services.AddTransient<IPatientAgentCapability, PrescriptionAgentCapability>();
            _ = services.AddSingleton(TimeProvider.System);
            _ = services.AddTransient<IClinicalEventProcedureBoundary, SeaClinicalEventProcedureBoundary>();
            _ = services.AddTransient<IClinicalEventProposalStore, ClinicClinicalEventProposalStore>();
            _ = services.AddTransient<GetPatientProcedureContext>();
            _ = services.AddTransient<PrepareClinicalEvent>();
            _ = services.AddTransient<ConfirmClinicalEventProposal>();
            _ = services.AddTransient<GetPatientProcedureContextTool>();
            _ = services.AddTransient<PrepareClinicalEventTool>();
            _ = services.AddTransient<IPatientAgentCapability, ClinicalEventsPatientAgentCapability>();
            _ = services.AddTransient<PatientAgentCapabilityRegistry>();
            _ = services.AddTransient<PatientAgentToolDispatcher>();
            _ = services.AddTransient<PatientAgentInstructionComposer>();
            _ = services.AddTransient<PatientWorkspaceAgentDefinitionFactory>();

            return services;
        }
    }
}
