using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Handlers;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Appointments;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Prescriptions;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.ClinicalEvents;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.ClinicalEvents;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Infrastructure.PatientAgent.Actions;
using Vitarish.Clinic.Infrastructure.PatientAgent;
using Vitarish.Clinic.Infrastructure.PatientAgent.Capabilities.Operations;
using Vitarish.Clinic.Infrastructure.PatientAgent.Operations;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Appointments.Application.AgentCapabilities;

namespace Vitarish.Clinic.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddClinicInfrastructure(this IServiceCollection services)
        {
            _ = services.AddSingleton(serviceProvider => new PatientWorkspaceAgentOptions(
                serviceProvider.GetRequiredService<IConfiguration>()["Vitarish:Intelligence:DefaultResponseModel"]
                ?? throw new InvalidOperationException("Vitarish:Intelligence:DefaultResponseModel must be configured.")));
            _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                typeof(ClinicDbContext).Assembly,
                typeof(FindOrCreatePatientHandler).Assembly));
            _ = services.AddTransient<IPatientDirectory, SeaPatientDirectory>();
            _ = services.AddTransient<IClinicActorContext, RequestClinicActorContext>();
            _ = services.AddTransient<IPatientAgentContextFactory, HttpPatientAgentContextFactory>();
            _ = services.AddTransient<IVisitsEventWriter, SeaVisitsEventWriter>();
            _ = services.AddTransient<IPrescriptionDraftWriter, MedicalPrescriptionDraftWriter>();
            _ = services.AddTransient<CreatePatientVisitsProcedureHandler>();
            _ = services.AddTransient<PatientViewQueryShape>();
            _ = services.AddTransient<PatientViewStatsLoader>();
            _ = services.AddSingleton<PatientAgentAuthorizationPolicy>();
            _ = services.AddSingleton<PatientAgentToolAvailabilityPolicy>();
            _ = services.AddSingleton<PatientAgentApprovalPolicy>();
            _ = services.AddTransient<CheckAppointmentAvailabilityTool>();
            _ = services.AddTransient<PrepareAppointmentTool>();
            _ = services.AddTransient<ConfirmAppointmentTool>();
            _ = services.AddTransient<PatientAppointmentQueryService>();
            _ = services.AddTransient<GetNextPatientAppointmentTool>();
            _ = services.AddTransient<ListPatientAppointmentsTool>();
            _ = services.AddTransient<CountPatientAppointmentsTool>();
            _ = services.AddTransient<IPatientAgentCapability, AppointmentAgentCapability>();
            _ = services.AddTransient<SearchPrescriptionTemplatesTool>();
            _ = services.AddTransient<GetPrescriptionTemplateRequirementsTool>();
            _ = services.AddTransient<PreparePrescriptionTool>();
            _ = services.AddTransient<ConfirmPrescriptionTool>();
            _ = services.AddTransient<IPatientAgentCapability, PrescriptionAgentCapability>();
            services.TryAddSingleton(TimeProvider.System);
            _ = services.AddTransient<IClinicalEventProcedureBoundary, SeaClinicalEventProcedureBoundary>();
            _ = services.AddSingleton<IClinicalEventProposalStore, RedisClinicalEventProposalStore>();
            _ = services.AddTransient<GetPatientProcedureContext>();
            _ = services.AddTransient<PrepareClinicalEvent>();
            _ = services.AddTransient<ConfirmClinicalEventProposal>();
            _ = services.AddTransient<GetPatientProcedureContextTool>();
            _ = services.AddTransient<PrepareClinicalEventTool>();
            _ = services.AddTransient<IPatientAgentCapability, ClinicalEventAgentCapability>();
            _ = services.AddTransient<IPatientAgentPatientSearch, ClinicPatientAgentSearch>();
            _ = services.AddTransient<IPatientAgentOperationsAccessVerifier, UserPatientAgentOperationsAccessVerifier>();
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
            _ = services.AddTransient<SeaManagementTool>();
            _ = services.AddTransient<IPatientAgentCapability, SeaManagementAgentCapability>();
            foreach (string toolName in new[] { "prepare_book_appointment", "prepare_reschedule_appointment", "prepare_cancel_appointment", "prepare_reject_appointment", "prepare_create_shift", "prepare_edit_shift", "prepare_close_shift", "prepare_cancel_shift", "prepare_archive_shift" })
            {
                string capturedName = toolName;
                _ = services.AddTransient<IClinicOperationsAgentTool>(provider =>
                    new PrepareScheduleActionTool(
                        provider.GetRequiredService<IAppointmentSchedulingAgentCapability>(),
                        provider.GetRequiredService<IPatientAgentPatientSearch>(), capturedName));
            }
            _ = services.AddTransient<IPatientAgentCapability, OperationsAgentCapability>();
            _ = services.AddTransient<PatientAgentCapabilityRegistry>();
            _ = services.AddTransient<PatientAgentToolDispatcher>();
            _ = services.AddTransient<PatientAgentInstructionComposer>();
            _ = services.AddTransient<PatientWorkspaceAgentDefinitionFactory>();
            _ = services.AddSingleton<IPatientAgentActionStore, RedisPatientAgentActionStore>();
            _ = services.AddTransient<IPatientAgentActionCommitter, AppointmentPatientAgentActionCommitter>();
            _ = services.AddTransient<IPatientAgentActionCommitter, PrescriptionPatientAgentActionCommitter>();
            _ = services.AddTransient<IPatientAgentActionCommitter, ClinicalEventPatientAgentActionCommitter>();
            _ = services.AddTransient<IPatientAgentActionCommitter, OperationsPatientAgentActionCommitter>();
            _ = services.AddTransient<IPatientAgentActionCommitter, SeaManagementPatientAgentActionCommitter>();
            _ = services.AddTransient<IPatientAgentActionCommitterRegistry, PatientAgentActionCommitterRegistry>();

            return services;
        }
    }
}
