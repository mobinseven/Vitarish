using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Appointments;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Prescriptions;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.ClinicalEvents;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Actions;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Operations;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Sea;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Communications;
using Vitarish.Clinic.Infrastructure.ClinicWorkspace.Operations;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
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
            _ = services.AddTransient<IClinicWorkspaceContextFactory, HttpClinicWorkspaceContextFactory>();
            _ = services.AddTransient<IClinicWorkspaceRoutePatientResolver, ClinicRoutePatientResolver>();
            _ = services.AddTransient<IClinicWorkspacePatientAccessVerifier, ClinicWorkspacePatientAccessVerifier>();
            _ = services.AddTransient<IVisitsEventWriter, SeaVisitsEventWriter>();
            _ = services.AddTransient<IPrescriptionDraftWriter, MedicalPrescriptionDraftWriter>();
            _ = services.AddTransient<CreatePatientVisitsProcedureHandler>();
            _ = services.AddTransient<PatientViewQueryShape>();
            _ = services.AddTransient<PatientViewStatsLoader>();
            _ = services.AddSingleton<ClinicWorkspaceAuthorizationPolicy>();
            _ = services.AddSingleton<ClinicWorkspaceToolAvailabilityPolicy>();
            _ = services.AddTransient<CheckAppointmentAvailabilityTool>();
            _ = services.AddTransient<PrepareAppointmentTool>();
            _ = services.AddTransient<ConfirmAppointmentTool>();
            _ = services.AddTransient<PatientAppointmentQueryService>();
            _ = services.AddTransient<GetNextPatientAppointmentTool>();
            _ = services.AddTransient<ListPatientAppointmentsTool>();
            _ = services.AddTransient<CountPatientAppointmentsTool>();
            _ = services.AddTransient<IClinicWorkspaceCapability, AppointmentAgentCapability>();
            _ = services.AddTransient<IClinicWorkspaceCapability, AppointmentReadAgentCapability>();
            _ = services.AddTransient<SearchPrescriptionTemplatesTool>();
            _ = services.AddTransient<GetPrescriptionTemplateRequirementsTool>();
            _ = services.AddTransient<ListPatientPrescriptionHistoryTool>();
            _ = services.AddTransient<GetPatientPrescriptionTool>();
            _ = services.AddTransient<GetPatientPrescriptionAttentionTool>();
            _ = services.AddTransient<PreparePrescriptionTool>();
            _ = services.AddTransient<ConfirmPrescriptionTool>();
            _ = services.AddTransient<IClinicWorkspaceCapability, PrescriptionAgentCapability>();
            _ = services.AddTransient<IClinicWorkspaceCapability, PrescriptionReadAgentCapability>();
            _ = services.AddTransient<ListPatientReminderStatusTool>();
            _ = services.AddTransient<IClinicWorkspaceCapability, PatientReminderReadAgentCapability>();
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
            _ = services.AddTransient<IClinicWorkspaceCapability, ClinicPatientReadAgentCapability>();
            _ = services.AddTransient<ListPatientProceduresTool>();
            _ = services.AddTransient<ListPatientProcedureEventsTool>();
            _ = services.AddTransient<GetPatientProcedureEventTool>();
            _ = services.AddTransient<ListPatientEventAttachmentMetadataTool>();
            _ = services.AddTransient<IClinicWorkspaceCapability, SeaPatientReadAgentCapability>();
            _ = services.AddTransient<IClinicWorkspaceCapability, ClinicalEventAgentCapability>();
            _ = services.AddTransient<IClinicWorkspacePatientSearch, ClinicWorkspaceSearch>();
            _ = services.AddTransient<IClinicWorkspaceOperationsAccessVerifier, UserClinicWorkspaceOperationsAccessVerifier>();
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
            _ = services.AddTransient<IClinicWorkspaceCapability, SeaManagementAgentCapability>();
            foreach (string toolName in new[] { "prepare_book_appointment", "prepare_reschedule_appointment", "prepare_cancel_appointment", "prepare_reject_appointment", "prepare_create_shift", "prepare_edit_shift", "prepare_close_shift", "prepare_cancel_shift", "prepare_archive_shift" })
            {
                string capturedName = toolName;
                _ = services.AddTransient<IClinicOperationsAgentTool>(provider =>
                    new PrepareScheduleActionTool(
                        provider.GetRequiredService<IAppointmentSchedulingAgentCapability>(),
                        provider.GetRequiredService<IClinicWorkspacePatientSearch>(), capturedName));
            }
            _ = services.AddTransient<IClinicWorkspaceCapability, OperationsAgentCapability>();
            _ = services.AddTransient<ClinicWorkspaceCapabilityRegistry>();
            _ = services.AddTransient<ClinicWorkspaceToolDispatcher>();
            _ = services.AddSingleton<IClinicWorkspaceActionStore, RedisClinicWorkspaceActionStore>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, AppointmentClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, PrescriptionClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, ClinicalEventClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, OperationsClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, SeaManagementClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, AppointmentAdministrationClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitter, ReminderManagementClinicWorkspaceActionCommitter>();
            _ = services.AddTransient<IClinicWorkspaceActionCommitterRegistry, ClinicWorkspaceActionCommitterRegistry>();

            return services;
        }
    }
}
