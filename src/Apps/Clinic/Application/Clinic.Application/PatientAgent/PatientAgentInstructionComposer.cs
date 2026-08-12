using System.Text;
using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class PatientAgentInstructionComposer
{
    public string Compose(
        PatientAgentContext context,
        IReadOnlyCollection<IPatientAgentCapability> capabilities,
        DateTimeOffset currentUtc)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        return "Agent policy (JSON):\n" + JsonSerializer.Serialize(new
        {
            subject = context.SubjectKind,
            patientId = context.SubjectKind == "patient" ? context.PatientId.ToString("D") : null,
            scope = context.SubjectKind == "practice" ? "authenticated_practice" : "server_bound_patient",
            patientScope = context.SubjectKind == "patient"
                ? $"immutable patient id is {context.PatientId:D}; it is server-bound from the route, and model or user input cannot change or override it"
                : null,
            patientSelection = context.SubjectKind == "practice" ? "bounded_search_opaque_token_only" : null,
            currentLocal = currentLocal.ToString("O"),
            currentUtc = currentUtc.ToString("O"),
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
            capabilities = capabilities.OrderBy(item => item.Name, StringComparer.Ordinal).Select(capability => new
            {
                name = capability.Name,
                description = capability.Description,
                instructions = capability.GetInstructions(),
                actions = capability.ActionTypes.OrderBy(action => action, StringComparer.Ordinal).ToArray()
            })
        });
    }
}
