using System.Text.Json;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

/// <summary>The trusted scope of a surgery tool call, read from the server-built execution metadata.</summary>
internal sealed record SurgeryAssistantScope(string PracticeId, string UserId, string AgentRunId, string SubjectKind, Guid PatientId)
{
    public const string PatientSubject = "patient";
    public const string PracticeSubject = "practice";

    public bool IsPatient => SubjectKind == PatientSubject;

    public static SurgeryAssistantScope From(AgentExecutionContext context)
    {
        string subjectKind = Optional(context, "subjectKind") ?? PatientSubject;
        Guid patientId = Guid.TryParse(Optional(context, "patientId"), out Guid parsed) ? parsed : Guid.Empty;
        if (subjectKind == PatientSubject && patientId == Guid.Empty)
            throw new InvalidOperationException("The trusted surgery patient scope is incomplete.");
        return new(Required(context, "practiceId"), Required(context, "authenticatedUserId"), context.ExecutionId, subjectKind, patientId);
    }

    /// <summary>The route-bound patient; practice-scope tools never call this.</summary>
    public static Guid PatientIdOf(AgentExecutionContext context) => From(context) is { IsPatient: true } scope
        ? scope.PatientId
        : throw new InvalidOperationException("The trusted surgery patient scope is incomplete.");

    private static string Required(AgentExecutionContext context, string key) =>
        Optional(context, key) ?? throw new InvalidOperationException($"The surgery assistant scope is missing {key}.");

    private static string? Optional(AgentExecutionContext context, string key) =>
        context.Metadata is not null && context.Metadata.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}

/// <summary>Reads the surgery tools' JSON arguments; a missing optional value reads as null so an edit can keep the current one.</summary>
internal static class SurgeryAssistantArguments
{
    public static string? Text(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public static string RequiredText(JsonElement arguments, string name) =>
        SurgeryPlanRules.NormalizeText(Text(arguments, name)) ?? throw new ArgumentException($"{name} is required.", name);

    public static Guid RequiredGuid(JsonElement arguments, string name) =>
        Guid.TryParse(Text(arguments, name), out Guid value) ? value : throw new ArgumentException($"{name} must be an id.", name);

    public static Guid? OptionalGuid(JsonElement arguments, string name) =>
        Text(arguments, name) is { } text ? Guid.TryParse(text, out Guid value) ? value : throw new ArgumentException($"{name} must be an id.", name) : null;

    public static DateTimeOffset? Instant(JsonElement arguments, string name) =>
        Text(arguments, name) is { } text
            ? DateTimeOffset.TryParse(text, out DateTimeOffset value) ? value : throw new ArgumentException($"{name} must be an ISO 8601 date and time with an offset.", name)
            : null;

    public static int? Integer(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

    public static bool Has(JsonElement arguments, string name) => arguments.TryGetProperty(name, out _);

    public static IReadOnlyList<string>? Strings(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => value.GetString()?.Trim() ?? string.Empty).Where(value => value.Length > 0).ToArray()
            : null;

    public static IReadOnlyList<SurgeryReminderPolicy>? Reminders(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryReminderPolicy(
                value.GetProperty("name").GetString() ?? string.Empty,
                value.GetProperty("minutesBefore").GetInt32(),
                value.GetProperty("subject").GetString() ?? string.Empty,
                value.GetProperty("textBody").GetString() ?? string.Empty)).ToArray()
            : null;

    public static IReadOnlyList<SurgeryTeamMember>? Team(JsonElement arguments) =>
        arguments.TryGetProperty("team", out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryTeamMember(
                value.GetProperty("userId").GetString()?.Trim() ?? string.Empty,
                string.Empty,
                value.GetProperty("role").GetString()?.Trim() ?? string.Empty)).ToArray()
            : null;

    public static IReadOnlyList<SurgeryContact>? Contacts(JsonElement arguments) =>
        arguments.TryGetProperty("contacts", out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryContact(
                value.GetProperty("name").GetString() ?? string.Empty,
                value.GetProperty("destination").GetString() ?? string.Empty,
                value.GetProperty("channel").GetString() ?? string.Empty)).ToArray()
            : null;

    /// <summary>The status an edit sets; only planned and completed are accepted, as in the plan form.</summary>
    public static SurgeryStatus? EditedStatus(JsonElement arguments) => Text(arguments, "status") switch
    {
        null => null,
        "planned" => SurgeryStatus.Planned,
        "completed" => SurgeryStatus.Completed,
        _ => throw new ArgumentException("status must be planned or completed.", "status")
    };
}

/// <summary>JSON schema fragments shared by the surgery tools.</summary>
internal static class SurgeryAssistantSchemas
{
    public const string Empty = """{"type":"object","properties":{},"additionalProperties":false}""";

    private const string Team = """ "team":{"type":"array","description":"Clinic staff from the staff list; each member once.","items":{"type":"object","properties":{"userId":{"type":"string"},"role":{"type":"string"}},"required":["userId","role"],"additionalProperties":false}}""";
    private const string Reminders = """ "reminderPolicies":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"minutesBefore":{"type":"integer","minimum":0},"subject":{"type":"string"},"textBody":{"type":"string"}},"required":["name","minutesBefore","subject","textBody"],"additionalProperties":false}}""";
    private const string PlanFields =
        """ "catalogId":{"type":"string","description":"A clinic surgery type id from the type list."},"name":{"type":"string"},"scheduledAt":{"type":"string","description":"ISO 8601 date and time with an offset."},"durationMinutes":{"type":"integer","minimum":1,"maximum":1440},"location":{"type":"string"},""" +
        Team + "," +
        """ "tools":{"type":"array","items":{"type":"string"}},""" +
        Reminders;
    private const string LegacyFields =
        """ "personsToNotify":{"type":"array","items":{"type":"string"},"description":"Optional legacy field; reminders are sent automatically to the patient and team."},"contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"destination":{"type":"string"},"channel":{"type":"string","enum":["email","sms"]}},"required":["name","destination","channel"],"additionalProperties":false},"description":"Optional legacy field."}""";

    public static readonly string PatientPlan =
        "{\"type\":\"object\",\"properties\":{" + PlanFields + "," + LegacyFields + "},\"required\":[\"catalogId\",\"name\",\"scheduledAt\"],\"additionalProperties\":false}";

    public static readonly string PracticePlan =
        "{\"type\":\"object\",\"properties\":{\"candidateToken\":{\"type\":\"string\",\"description\":\"The patient's opaque token from search_patients.\"}," + PlanFields + "},\"required\":[\"candidateToken\",\"catalogId\",\"name\",\"scheduledAt\"],\"additionalProperties\":false}";

    public static readonly string Edit =
        "{\"type\":\"object\",\"properties\":{\"surgeryId\":{\"type\":\"string\"}," + PlanFields +
        ",\"status\":{\"type\":\"string\",\"enum\":[\"planned\",\"completed\"],\"description\":\"Set completed back to planned to correct a mistaken completion.\"}},\"required\":[\"surgeryId\"],\"additionalProperties\":false}";

    public const string SurgeryId = """{"type":"object","properties":{"surgeryId":{"type":"string"}},"required":["surgeryId"],"additionalProperties":false}""";

    public const string Calendar = """{"type":"object","properties":{"from":{"type":"string","description":"ISO 8601 start, inclusive."},"to":{"type":"string","description":"ISO 8601 end, exclusive; at most 93 days after from."},"status":{"type":"string","enum":["planned","completed","cancelled"]}},"required":["from","to"],"additionalProperties":false}""";

    public const string Staff = """{"type":"object","properties":{"search":{"type":"string","maxLength":100}},"additionalProperties":false}""";

    public const string Conflicts = """{"type":"object","properties":{"scheduledAt":{"type":"string"},"durationMinutes":{"type":"integer","minimum":1,"maximum":1440},"location":{"type":"string"},"teamUserIds":{"type":"array","items":{"type":"string"}},"excludeSurgeryId":{"type":"string","description":"The surgery being edited."}},"required":["scheduledAt"],"additionalProperties":false}""";

    public static readonly string TypeCreation =
        "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"tools\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"defaultTeamRoles\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}," + Reminders + "},\"required\":[\"name\"],\"additionalProperties\":false}";

    public static readonly string TypeUpdate =
        "{\"type\":\"object\",\"properties\":{\"surgeryTypeId\":{\"type\":\"string\"},\"name\":{\"type\":\"string\"},\"tools\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"defaultTeamRoles\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}," + Reminders + "},\"required\":[\"surgeryTypeId\"],\"additionalProperties\":false}";

    public const string TypeId = """{"type":"object","properties":{"surgeryTypeId":{"type":"string"}},"required":["surgeryTypeId"],"additionalProperties":false}""";

    public const string CancellationMessage = """{"type":"object","properties":{"subject":{"type":"string","maxLength":200},"textBody":{"type":"string","maxLength":1000}},"required":["subject","textBody"],"additionalProperties":false}""";
}
