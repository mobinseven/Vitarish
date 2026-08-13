using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

internal abstract class ClinicAgentProfile(
    ClinicAgentCompositionContext context,
    PatientWorkspaceAgentOptions options,
    IReadOnlyList<string> capabilityKeys) : AgentProfile<ClinicAgentCompositionContext>
{
    protected override void Configure(AgentProfileBuilder<ClinicAgentCompositionContext> builder)
    {
        builder.ApplyLayer("clinic.policy", 1, policy =>
        {
            ClinicAgentCompositionContext profileComposition = context with
            {
                AvailableCapabilities = context.AvailableCapabilities
                    .Where(capability => capabilityKeys.Contains(capability.Name, StringComparer.Ordinal))
                    .ToArray()
            };
            policy.AddInstruction(
                "clinic.policy",
                ClinicAgentPolicyInstructions.Create(profileComposition, dynamicTime: true),
                0,
                ClinicAgentPolicyInstructions.Create(profileComposition, dynamicTime: false));
            policy.AddCompatibilityMaterial("practiceId", context.Context.PracticeId);
            policy.AddCompatibilityMaterial("subjectKind", context.Context.SubjectKind);
            policy.AddCompatibilityMaterial("subjectId", SubjectId(context.Context));
            policy.AddCompatibilityMaterial("authenticatedUserId", context.Context.AuthenticatedUserId);
            policy.AddCompatibilityMaterial("role", context.Context.Role);
            policy.AddCompatibilityMaterial("locale", context.Context.Locale);
            policy.AddCompatibilityMaterial("timeZone", context.Context.TimeZone);
            policy.AddCompatibilityMaterial("calendar", context.Context.Calendar);
        });
        builder.ApplyLayer(ProfileLayerKey, 1, profile =>
        {
            profile.SetIdentity(AgentKey, DisplayName);
            profile.SetModel(options.Model);
            profile.SetResult(new AgentResultDefinition("patient_workspace_result", PatientWorkspaceAgentDefinition.ResultSchema));
            profile.SetLimits(new AgentExecutionLimits());
            foreach (string capabilityKey in capabilityKeys) profile.AddCapability(capabilityKey);
        });
    }

    protected abstract string AgentKey { get; }
    protected abstract string DisplayName { get; }
    protected abstract string ProfileLayerKey { get; }

    private static string SubjectId(PatientAgentContext context) =>
        context.SubjectKind == "practice" ? context.PracticeId : context.PatientId.ToString("D");
}

internal sealed class ClinicPatientAgentProfile(
    ClinicAgentCompositionContext context,
    PatientWorkspaceAgentOptions options)
    : ClinicAgentProfile(context, options, ClinicAgentCapabilityKeys.Patient)
{
    protected override string AgentKey => "clinic.patient";
    protected override string DisplayName => "Clinic patient workspace";
    protected override string ProfileLayerKey => "clinic.patient";
}

internal sealed class ClinicOperationsAgentProfile(
    ClinicAgentCompositionContext context,
    PatientWorkspaceAgentOptions options)
    : ClinicAgentProfile(context, options, ClinicAgentCapabilityKeys.OperationsProfile)
{
    protected override string AgentKey => "clinic.operations";
    protected override string DisplayName => "Clinic operations workspace";
    protected override string ProfileLayerKey => "clinic.operations";
}

internal static class ClinicAgentPolicyInstructions
{
    public static string Create(ClinicAgentCompositionContext composition, bool dynamicTime)
    {
        PatientAgentContext context = composition.Context;
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        DateTimeOffset currentUtc = dynamicTime ? composition.CurrentUtc : DateTimeOffset.UnixEpoch;
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        string currentUtcText = dynamicTime ? currentUtc.ToString("O") : "<current-utc>";
        string currentLocalText = dynamicTime ? currentLocal.ToString("O") : "<current-local>";
        string policy = JsonSerializer.Serialize(new
        {
            subject = context.SubjectKind,
            patientId = context.SubjectKind == "patient" ? context.PatientId.ToString("D") : null,
            scope = context.SubjectKind == "practice" ? "authenticated_practice" : "server_bound_patient",
            patientScope = context.SubjectKind == "patient"
                ? $"immutable patient id is {context.PatientId:D}; it is server-bound from the route, and model or user input cannot change or override it"
                : null,
            patientSelection = context.SubjectKind == "practice" ? "bounded_search_opaque_token_only" : null,
            currentLocal = currentLocalText,
            currentUtc = currentUtcText,
            responseLanguage = "user_input",
            localeFallback = context.Locale,
            timeZone = context.TimeZone,
            calendarFallback = context.Calendar,
            dateConversion = new
            {
                tool = "convert_calendar_date",
                sourceCalendar = "infer_from_user_input",
                outputField = "response_calendar.date",
                required = true
            },
            mutations = context.SubjectKind == "practice" ? "direct_allowlist_only_else_confirm" : "prepare_only_never_commit",
            clinicalConclusions = false,
            patientFacts = "use_a_fresh_authoritative_tool_read_for_each_patient_fact; select_only_relevant_tools; never_dump_an_unrequested_record",
            missingData = "ask_do_not_guess; dependency_unavailable_or_partial_is_not_absent_data",
            statusAnswers = "for_generic_status_lead_with_the_most_material_recorded_facts_and_dates; for_specific_questions_answer_directly",
            actionOutput = "one_distinct_action_per_request",
            machineValues = "do_not_expose_unless_requested",
            capabilities = composition.AvailableCapabilities.OrderBy(item => item.Name, StringComparer.Ordinal).Select(capability => new
            {
                name = capability.Name,
                description = capability.Description,
                instructions = capability.GetInstructions(),
                actions = capability.ActionTypes.OrderBy(action => action, StringComparer.Ordinal).ToArray()
            })
        });
        return "Agent policy (JSON):\n" + policy;
    }
}
