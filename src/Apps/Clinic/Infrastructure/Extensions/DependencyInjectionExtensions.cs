using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Policies;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Appointments;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Prescriptions;
using Vitarish.Clinic.Application.Events;
using Vitarish.Clinic.Infrastructure.Events;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Events;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Patients;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Sea;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Communications;
using Vitarish.Clinic.Infrastructure.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Appointments.Application.AgentCapabilities;

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
            _ = services.AddTransient<IClinicActorContext, RequestClinicActorContext>();
            _ = services.AddTransient<IAssistantContextFactory, HttpAssistantContextFactory>();
            _ = services.AddTransient<IAssistantRoutePatientResolver, ClinicRoutePatientResolver>();
            _ = services.AddTransient<IAssistantPatientAccessVerifier, AssistantPatientAccessVerifier>();
            _ = services.AddTransient<IVisitsEventWriter, SeaVisitsEventWriter>();
            _ = services.AddTransient<IPrescriptionDraftWriter, MedicalPrescriptionDraftWriter>();
            _ = services.AddTransient<CreatePatientVisitsProcedureHandler>();
            _ = services.AddTransient<PatientViewQueryShape>();
            _ = services.AddTransient<PatientViewStatsLoader>();
            _ = services.AddSingleton<AssistantAuthorizationPolicy>();
            _ = services.AddSingleton<AssistantToolAvailabilityPolicy>();
            _ = services.AddTransient<CheckAppointmentAvailabilityTool>();
            _ = services.AddTransient<PrepareAppointmentTool>();
            _ = services.AddTransient<ConfirmAppointmentTool>();
            _ = services.AddTransient<PatientAppointmentQueryService>();
            _ = services.AddTransient<GetNextPatientAppointmentTool>();
            _ = services.AddTransient<ListPatientAppointmentsTool>();
            _ = services.AddTransient<CountPatientAppointmentsTool>();
            _ = services.AddTransient<IAssistantCapability, AppointmentAgentCapability>();
            _ = services.AddTransient<IAssistantCapability, AppointmentReadAgentCapability>();
            _ = services.AddTransient<SearchPrescriptionTemplatesTool>();
            _ = services.AddTransient<GetPrescriptionTemplateRequirementsTool>();
            _ = services.AddTransient<ListPatientPrescriptionHistoryTool>();
            _ = services.AddTransient<GetPatientPrescriptionTool>();
            _ = services.AddTransient<GetPatientPrescriptionAttentionTool>();
            _ = services.AddTransient<PreparePrescriptionTool>();
            _ = services.AddTransient<ConfirmPrescriptionTool>();
            _ = services.AddTransient<IAssistantCapability, PrescriptionAgentCapability>();
            _ = services.AddTransient<IAssistantCapability, PrescriptionReadAgentCapability>();
            _ = services.AddTransient<ListPatientReminderStatusTool>();
            _ = services.AddTransient<IAssistantCapability, PatientReminderReadAgentCapability>();
            services.TryAddSingleton(TimeProvider.System);
            _ = services.AddTransient<IClinicalEventProcedureBoundary, SeaClinicalEventProcedureBoundary>();
            _ = services.AddSingleton<IClinicalEventProposalStore, RedisClinicalEventProposalStore>();
            _ = services.AddTransient<GetPatientProcedureContext>();
            _ = services.AddTransient<PrepareClinicalEvent>();
            _ = services.AddTransient<ConfirmClinicalEventProposal>();
            _ = services.AddTransient<GetPatientProcedureContextTool>();
            _ = services.AddTransient<PrepareClinicalEventTool>();
            _ = services.AddTransient<GetPatientFactualOverviewTool>();
            _ = services.AddTransient<GetPatientProfileTool>();
            _ = services.AddTransient<GetPatientRecordedClinicalFactsTool>();
            _ = services.AddTransient<IAssistantCapability, ClinicPatientReadAgentCapability>();
            _ = services.AddTransient<ListPatientProceduresTool>();
            _ = services.AddTransient<ListPatientProcedureEventsTool>();
            _ = services.AddTransient<GetPatientProcedureEventTool>();
            _ = services.AddTransient<ListPatientEventAttachmentMetadataTool>();
            _ = services.AddTransient<IAssistantCapability, SeaPatientReadAgentCapability>();
            _ = services.AddTransient<IAssistantCapability, ClinicalEventAgentCapability>();
            _ = services.AddTransient<IAssistantPatientSearch, AssistantSearch>();
            _ = services.AddTransient<IAssistantOperationsAccessVerifier, UserAssistantOperationsAccessVerifier>();
            _ = services.AddTransient<IClinicOperationsAgentTool, SearchPatientsTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ListOperationsAppointmentsTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ListOperationsShiftsTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, CountOperationsAppointmentsTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, GetOperationsAppointmentTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, GetOperationsAvailabilityTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, GetOperationsShiftTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, GetOperationsOccurrencesTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ResolveOperationsServicesTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ResolveOperationsServiceTypesTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ResolveOperationsProvidersTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ConfirmOperationTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, PresentOperationTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, CompleteOperationTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ActivateShiftTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, PauseShiftTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, AppointmentAdministrationAgentTool>();
            _ = services.AddTransient<IClinicOperationsAgentTool, ReminderManagementAgentTool>();
            _ = services.AddTransient<SeaManagementClinicToolAdapter>();
            _ = services.AddTransient<AppointmentAdministrationAgentTool>();
            _ = services.AddTransient<ReminderManagementAgentTool>();
            _ = services.AddSingleton<IReminderSourceRegistry, ReminderSourceRegistry>();
            _ = services.AddSingleton<IReminderSourceDefinition, AppointmentReminderSourceDefinition>();
            _ = services.AddSingleton<IReminderSourceDefinition, SeaReminderSourceDefinition>();
            _ = services.AddTransient<IAssistantCapability, SeaManagementAgentCapability>();
            foreach (string toolName in new[] { "prepare_book_appointment", "prepare_reschedule_appointment", "prepare_cancel_appointment", "prepare_reject_appointment", "prepare_create_shift", "prepare_edit_shift", "prepare_close_shift", "prepare_cancel_shift", "prepare_archive_shift" })
            {
                string capturedName = toolName;
                _ = services.AddTransient<IClinicOperationsAgentTool>(provider =>
                    new PrepareScheduleActionTool(
                        provider.GetRequiredService<IAppointmentSchedulingAgentCapability>(),
                        provider.GetRequiredService<IAssistantPatientSearch>(), capturedName));
            }
            _ = services.AddTransient<IAssistantCapability, OperationsAgentCapability>();
            _ = services.AddTransient<AssistantCapabilityRegistry>();
            _ = services.AddTransient<AssistantToolDispatcher>();
            _ = services.AddSingleton<IAssistantActionStore, RedisAssistantActionStore>();
            _ = services.AddTransient<IAssistantActionCommitter, AppointmentAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, PrescriptionAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, ClinicalEventAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, OperationsAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, SeaManagementAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, AppointmentAdministrationAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitter, ReminderManagementAssistantActionCommitter>();
            _ = services.AddTransient<IAssistantActionCommitterRegistry, AssistantActionCommitterRegistry>();

            return services;
        }
    }
}
