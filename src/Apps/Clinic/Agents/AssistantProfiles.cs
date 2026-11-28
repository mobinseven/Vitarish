using System.Globalization;
using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

internal abstract class AssistantProfile(
    AssistantCompositionContext context,
    AssistantOptions options,
    IReadOnlyList<string> capabilityKeys) : AgentProfile<AssistantCompositionContext>
{
    protected override void Configure(AgentProfileBuilder<AssistantCompositionContext> builder)
    {
        builder.ApplyLayer("clinic.policy", 1, policy =>
        {
            AssistantCompositionContext profileComposition = context with
            {
                AvailableCapabilities = context.AvailableCapabilities
                    .Where(capability => capabilityKeys.Contains(capability.Name, StringComparer.Ordinal))
                    .ToArray()
            };
            policy.AddInstruction(
                "clinic.policy",
                PolicyInstructions(profileComposition, dynamicTime: true),
                0,
                PolicyInstructions(profileComposition, dynamicTime: false));
            policy.AddCompatibilityMaterial("practiceId", context.Context.PracticeId);
            policy.AddCompatibilityMaterial("subjectKind", context.Context.SubjectKind);
            policy.AddCompatibilityMaterial("subjectId", SubjectId(context.Context));
            policy.AddCompatibilityMaterial("authenticatedUserId", context.Context.AuthenticatedUserId);
            policy.AddCompatibilityMaterial("role", context.Context.Role);
            policy.AddCompatibilityMaterial(
                "actionPolicies",
                FormatActionPolicies(profileComposition.AvailableCapabilities.SelectMany(capability => capability.ActionPolicies)));
        });
        builder.ApplyLayer(ProfileLayerKey, 1, profile =>
        {
            profile.SetIdentity(AgentKey, DisplayName);
            profile.SetModel(options.Model);
            profile.SetResult(new AgentResultDefinition("clinic_assistant_result", ResultSchema));
            profile.SetLimits(Limits);
            if (IncludesDomainModel)
                profile.SetDomainModel(ClinicAssistantDomainModel.Create(), ClinicAssistantDomainModel.InstructionOrder);
            foreach (string capabilityKey in capabilityKeys) profile.AddCapability(capabilityKey);
        });
    }

    protected abstract string AgentKey { get; }
    protected abstract string DisplayName { get; }
    protected abstract string ProfileLayerKey { get; }
    protected virtual string ResultSchema => AssistantRuntime.ResultSchema;
    protected virtual AgentExecutionLimits Limits => new(MaxTokens: 128_000);
    protected virtual bool IncludesDomainModel => true;
    protected virtual string PolicyInstructions(AssistantCompositionContext composition, bool dynamicTime) =>
        AssistantPolicyInstructions.Create(composition, dynamicTime);

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
    AssistantCompositionContext context,
    AssistantOptions options)
    : AssistantProfile(context, options, AssistantCapabilityKeys.Patient)
{
    protected override string AgentKey => AssistantSubjects.PatientAgentKey;
    protected override string DisplayName => "Clinic patient workspace";
    protected override string ProfileLayerKey => "clinic.assistant.patient";
}

internal sealed class ClinicOperationsAssistantProfile(
    AssistantCompositionContext context,
    AssistantOptions options)
    : AssistantProfile(context, options, AssistantCapabilityKeys.OperationsProfile)
{
    protected override string AgentKey => AssistantSubjects.OperationsAgentKey;
    protected override string DisplayName => "Clinic operations workspace";
    protected override string ProfileLayerKey => "clinic.assistant.operations";
    protected override string ResultSchema => AssistantRuntime.OperationsResultSchema;

    protected override string PolicyInstructions(AssistantCompositionContext composition, bool dynamicTime)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(composition.Context.TimeZone);
        DateOnly localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(composition.CurrentUtc, timeZone).DateTime);
        string relativeDates = JsonSerializer.Serialize(new
        {
            yesterday = dynamicTime ? localDate.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "<local-yesterday>",
            today = dynamicTime ? localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "<local-today>",
            tomorrow = dynamicTime ? localDate.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "<local-tomorrow>"
        });
        return base.PolicyInstructions(composition, dynamicTime) +
            "\nPractice-local Gregorian dates: " + relativeDates +
            "\nFor yesterday, today, or tomorrow, pass the corresponding Gregorian date above directly to scheduling tools. Use the calendar tool only to convert an exact date between calendars.";
    }
}

/// <summary>
/// The read-only operations-help entry: one model round that answers a workflow question from the
/// app-owned instructions alone. It composes no capability, so it has no practice-record tools, no
/// mutation capability and no way to propose an action.
/// </summary>
internal sealed class ClinicOperationsHelpProfile(
    AssistantCompositionContext context,
    AssistantOptions options)
    : AssistantProfile(context, options, [])
{
    protected override string AgentKey => AssistantSubjects.OperationsHelpAgentKey;
    protected override string DisplayName => "Clinic operations help";
    protected override string ProfileLayerKey => AssistantSubjects.OperationsHelpAgentKey;
    protected override string ResultSchema => AssistantRuntime.OperationsHelpResultSchema;
    protected override AgentExecutionLimits Limits => new(MaxIterations: 1, MaxToolCalls: 0, MaxTokens: 128_000);
    protected override bool IncludesDomainModel => false;

    protected override string PolicyInstructions(AssistantCompositionContext composition, bool dynamicTime)
    {
        string policy = JsonSerializer.Serialize(new
        {
            subject = composition.Context.SubjectKind,
            scope = "operations_workflow_help",
            responseLanguage = "user_input",
            topics = "how_to_book_a_visit_see_the_schedule_confirm_or_delete_a_reservation_and_create_edit_or_delete_a_shift",
            practiceRecords = "not_available_in_this_mode; never_state_or_guess_a_specific_patient_appointment_shift_or_count",
            actions = "none; explain_the_steps_the_user_performs_on_the_screen_and_never_claim_to_have_changed_or_prepared_anything",
            screenSteps = "use_only_the_steps_and_names_of_the_workflow_guide; if_the_question_is_not_covered_or_you_are_unsure_say_so_and_never_invent_a_screen_button_or_feature",
            clinicalConclusions = false,
            unknown = "say_so_and_suggest_using_the_assistant_for_practice_data",
            result = $"write_the_whole_reply_in_the_{AssistantRuntime.OperationsHelpAnswerField}_field_as_plain_text"
        });
        return "Agent policy (JSON):\n" + policy + "\n\n" + OperationsHelpGuide.Instructions;
    }
}

internal static class AssistantPolicyInstructions
{
    public static string Create(AssistantCompositionContext composition, bool dynamicTime)
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
            // Locale, time zone and calendar are sent with every turn and do not change the configuration key.
            localeFallback = dynamicTime ? context.Locale : "<locale>",
            timeZone = dynamicTime ? context.TimeZone : "<time-zone>",
            calendarFallback = dynamicTime ? context.Calendar : "<calendar>",
            dateConversion = new
            {
                tool = CalendarDateConversionTool.Name,
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
