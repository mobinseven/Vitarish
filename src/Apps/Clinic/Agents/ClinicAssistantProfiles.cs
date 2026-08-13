using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

internal abstract class ClinicAssistantProfile(
    ClinicAssistantCompositionContext context,
    ClinicAssistantOptions options,
    IReadOnlyList<string> capabilityKeys) : AgentProfile<ClinicAssistantCompositionContext>
{
    protected override void Configure(AgentProfileBuilder<ClinicAssistantCompositionContext> builder)
    {
        builder.ApplyLayer("clinic.policy", 1, policy =>
        {
            ClinicAssistantCompositionContext profileComposition = context with
            {
                AvailableCapabilities = context.AvailableCapabilities
                    .Where(capability => capabilityKeys.Contains(capability.Name, StringComparer.Ordinal))
                    .ToArray()
            };
            policy.AddInstruction(
                "clinic.policy",
                ClinicAssistantPolicyInstructions.Create(profileComposition, dynamicTime: true),
                0,
                ClinicAssistantPolicyInstructions.Create(profileComposition, dynamicTime: false));
            policy.AddCompatibilityMaterial("practiceId", context.Context.PracticeId);
            policy.AddCompatibilityMaterial("subjectKind", context.Context.SubjectKind);
            policy.AddCompatibilityMaterial("subjectId", SubjectId(context.Context));
            policy.AddCompatibilityMaterial("authenticatedUserId", context.Context.AuthenticatedUserId);
            policy.AddCompatibilityMaterial("role", context.Context.Role);
            policy.AddCompatibilityMaterial("locale", context.Context.Locale);
            policy.AddCompatibilityMaterial("timeZone", context.Context.TimeZone);
            policy.AddCompatibilityMaterial("calendar", context.Context.Calendar);
            policy.AddCompatibilityMaterial(
                "actionPolicies",
                FormatActionPolicies(profileComposition.AvailableCapabilities.SelectMany(capability => capability.ActionPolicies)));
        });
        builder.ApplyLayer(ProfileLayerKey, 1, profile =>
        {
            profile.SetIdentity(AgentKey, DisplayName);
            profile.SetModel(options.Model);
            profile.SetResult(new AgentResultDefinition("clinic_assistant_result", ClinicAssistantRuntime.ResultSchema));
            profile.SetLimits(new AgentExecutionLimits());
            foreach (string capabilityKey in capabilityKeys) profile.AddCapability(capabilityKey);
        });
    }

    protected abstract string AgentKey { get; }
    protected abstract string DisplayName { get; }
    protected abstract string ProfileLayerKey { get; }

    private static string SubjectId(AssistantContext context) =>
        context.SubjectKind == "practice" ? context.PracticeId : context.PatientId.ToString("D");

    private static string FormatActionPolicy(AssistantActionPolicy item) => string.Join("|",
        item.ActionType,
        item.OwningWorkflow,
        item.ExecutionMode,
        item.Revision,
        item.RetryClassification,
        item.DefaultProposalLifetime?.Ticks.ToString() ?? "",
        string.Join(",", item.RequiredPermissions.OrderBy(value => value, StringComparer.Ordinal)),
        string.Join(",", item.RequiredRoles.OrderBy(value => value, StringComparer.Ordinal)),
        string.Join(",", item.ToolNames.OrderBy(value => value, StringComparer.Ordinal)));

    private static string FormatActionPolicies(IEnumerable<AssistantActionPolicy> policies)
    {
        string value = string.Join("\n", policies
            .OrderBy(item => item.ActionType, StringComparer.Ordinal)
            .Select(FormatActionPolicy));
        return string.IsNullOrEmpty(value) ? "none" : value;
    }
}

internal sealed class ClinicPatientAssistantProfile(
    ClinicAssistantCompositionContext context,
    ClinicAssistantOptions options)
    : ClinicAssistantProfile(context, options, ClinicAssistantCapabilityKeys.Patient)
{
    protected override string AgentKey => "clinic.assistant.patient";
    protected override string DisplayName => "Clinic patient workspace";
    protected override string ProfileLayerKey => "clinic.assistant.patient";
}

internal sealed class ClinicOperationsAssistantProfile(
    ClinicAssistantCompositionContext context,
    ClinicAssistantOptions options)
    : ClinicAssistantProfile(context, options, ClinicAssistantCapabilityKeys.OperationsProfile)
{
    protected override string AgentKey => "clinic.assistant.operations";
    protected override string DisplayName => "Clinic operations workspace";
    protected override string ProfileLayerKey => "clinic.assistant.operations";
}

internal static class ClinicAssistantPolicyInstructions
{
    public static string Create(ClinicAssistantCompositionContext composition, bool dynamicTime)
    {
        AssistantContext context = composition.Context;
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        DateTimeOffset currentUtc = dynamicTime ? composition.CurrentUtc : DateTimeOffset.UnixEpoch;
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        string currentUtcText = dynamicTime ? currentUtc.ToString("O") : "<current-utc>";
        string currentLocalText = dynamicTime ? currentLocal.ToString("O") : "<current-local>";
        string policy = JsonSerializer.Serialize(new
        {
            subject = context.SubjectKind,
            scope = context.SubjectKind == "practice" ? "authenticated_practice" : "server_bound_patient",
            patientScope = context.SubjectKind == "patient"
                ? "the patient is an opaque server-bound reference from the authenticated route; model or user input cannot change or override it"
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
                actions = capability.ActionPolicies
                    .Where(policy => policy.Authorizes(context))
                    .Select(policy => policy.ActionType)
                    .OrderBy(action => action, StringComparer.Ordinal)
                    .ToArray()
            })
        });
        return "Agent policy (JSON):\n" + policy;
    }
}
