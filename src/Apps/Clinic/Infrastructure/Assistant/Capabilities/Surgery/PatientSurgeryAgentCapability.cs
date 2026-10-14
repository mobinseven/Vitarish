using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

public static class SurgeryAssistantActions
{
    public const string Create = "create_patient_surgery";
    public const string Capability = "Surgeries";
    public const string Workflow = "surgeries";
    public const string ListTool = "list_patient_surgeries";
    public const string ListTypesTool = "list_surgery_types";
    public const string PrepareTool = "prepare_patient_surgery";
}

public sealed class PatientSurgeryAgentCapability(
    ListPatientSurgeriesAssistantTool list,
    ListSurgeryTypesAssistantTool listTypes,
    PreparePatientSurgeryTool prepare) : AssistantCapability([list, listTypes, prepare], [
        AssistantActionPolicies.Prepare(
            SurgeryAssistantActions.Create,
            SurgeryAssistantActions.Workflow,
            ["ClinicalEvents.Create"],
            toolNames: [SurgeryAssistantActions.PrepareTool],
            retryClassification: AssistantActionRetryClassification.Idempotent)
    ])
{
    public override string Name => SurgeryAssistantActions.Capability;
    public override string Description => "Read current-patient surgeries and prepare a surgery plan for explicit clinician confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read", "ClinicalEvents.Create" };
    public override string GetInstructions() =>
        $"Use only the server-supplied patient scope. Call {SurgeryAssistantActions.ListTypesTool} before preparing a plan so the clinician picks a valid surgery type; team members must be clinic staff user ids. " +
        "Reminders are sent automatically to the patient and every team member. Confirm the full surgery plan, contacts, and reminder policy with the clinician before preparing it; never create it during the model run.";
}

public sealed class ListPatientSurgeriesAssistantTool(ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.ListTool,
        "List surgeries for the current patient.",
        "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        Guid patientId = SurgeryAssistantScope.PatientId(context);
        IReadOnlyList<SurgeryReadModel> values = await surgeries.GetByPatientAsync(patientId, cancellationToken);
        return new(call.Id, JsonSerializer.Serialize(
            new SurgeryAssistantListResult(values),
            SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantListResult));
    }
}

public sealed class ListSurgeryTypesAssistantTool(ISurgeryCatalogStore catalogs) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.ListTypesTool,
        "List this clinic's surgery types with their default tools, team roles and reminders.",
        "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SurgeryCatalog> types = await catalogs.ListAsync(cancellationToken);
        return new(call.Id, JsonSerializer.Serialize(
            new SurgeryAssistantTypesResult(types),
            SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantTypesResult));
    }
}

public sealed class PreparePatientSurgeryTool(ISurgeryCatalogStore catalogs) : IAgentTool, IAssistantPreparationTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.PrepareTool,
        "Prepare a current-patient surgery plan for explicit clinician confirmation; this does not create it.",
        """{"type":"object","properties":{"catalogId":{"type":"string","description":"A clinic surgery type id; list the clinic's surgery types before choosing one."},"name":{"type":"string"},"scheduledAt":{"type":"string"},"durationMinutes":{"type":"integer","minimum":1,"maximum":1440},"location":{"type":"string"},"team":{"type":"array","items":{"type":"object","properties":{"userId":{"type":"string"},"role":{"type":"string"}},"required":["userId","role"],"additionalProperties":false}},"tools":{"type":"array","items":{"type":"string"}},"personsToNotify":{"type":"array","items":{"type":"string"},"description":"Optional legacy field; reminders are sent automatically to the patient and team."},"contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"destination":{"type":"string"},"channel":{"type":"string","enum":["email","sms"]}},"required":["name","destination","channel"],"additionalProperties":false},"description":"Optional legacy field."},"reminderPolicies":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"minutesBefore":{"type":"integer","minimum":0},"subject":{"type":"string"},"textBody":{"type":"string"}},"required":["name","minutesBefore","subject","textBody"],"additionalProperties":false}}},"required":["catalogId","name","scheduledAt"],"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement arguments = document.RootElement;
        string catalogId = arguments.GetProperty("catalogId").GetString() ?? string.Empty;
        SurgeryCatalog? catalog = await catalogs.FindAsync(catalogId, cancellationToken);
        if (catalog is null)
        {
            throw new ArgumentException("The surgery type does not exist.", nameof(catalogId));
        }
        SurgeryAssistantProposal proposal = new(
            Guid.CreateVersion7(),
            SurgeryAssistantScope.PatientId(context),
            catalogId,
            arguments.GetProperty("name").GetString() ?? string.Empty,
            arguments.GetProperty("scheduledAt").GetDateTimeOffset(),
            Strings(arguments, "tools"),
            Strings(arguments, "personsToNotify"),
            Contacts(arguments),
            Policies(arguments),
            Duration(arguments),
            Location(arguments),
            TeamMembers(arguments));
        return new AgentToolResult(
            call.Id,
            JsonSerializer.Serialize(
                new SurgeryAssistantPreparationResult(proposal),
                SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantPreparationResult));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        SurgeryAssistantPreparationResult? preparation = JsonSerializer.Deserialize(
            result.Output,
            SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantPreparationResult);
        return preparation?.Proposal is { } proposal
            ? new(
                SurgeryAssistantActions.Capability,
                SurgeryAssistantActions.Create,
                proposal.Id,
                JsonSerializer.Serialize(proposal, SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantProposal),
                DateTimeOffset.UtcNow.AddMinutes(30),
                proposal.Location is { Length: > 0 } location
                    ? $"Create surgery '{proposal.Name}' at {proposal.ScheduledAt:O} in {location}."
                    : $"Create surgery '{proposal.Name}' at {proposal.ScheduledAt:O}.")
            : null;
    }

    private static IReadOnlyList<string> Strings(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray()
            : [];

    private static IReadOnlyList<SurgeryContact> Contacts(JsonElement arguments) =>
        arguments.TryGetProperty("contacts", out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryContact(
                value.GetProperty("name").GetString() ?? string.Empty,
                value.GetProperty("destination").GetString() ?? string.Empty,
                value.GetProperty("channel").GetString() ?? string.Empty)).ToArray()
            : [];

    private static IReadOnlyList<SurgeryReminderPolicy> Policies(JsonElement arguments) =>
        arguments.TryGetProperty("reminderPolicies", out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryReminderPolicy(
                value.GetProperty("name").GetString() ?? string.Empty,
                value.GetProperty("minutesBefore").GetInt32(),
                value.GetProperty("subject").GetString() ?? string.Empty,
                value.GetProperty("textBody").GetString() ?? string.Empty)).ToArray()
            : [];

    private static int? Duration(JsonElement arguments) =>
        arguments.TryGetProperty("durationMinutes", out JsonElement value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static string? Location(JsonElement arguments) =>
        arguments.TryGetProperty("location", out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? SurgeryPlanRules.NormalizeText(value.GetString())
            : null;

    private static IReadOnlyList<SurgeryAssistantTeamMember> TeamMembers(JsonElement arguments) =>
        arguments.TryGetProperty("team", out JsonElement values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Select(value => new SurgeryAssistantTeamMember(
                value.GetProperty("userId").GetString() ?? string.Empty,
                value.GetProperty("role").GetString() ?? string.Empty)).ToArray()
            : [];
}

internal static class SurgeryAssistantScope
{
    public static Guid PatientId(AgentExecutionContext context) =>
        context.Metadata is not null && context.Metadata.TryGetValue("patientId", out string? value) && Guid.TryParse(value, out Guid patientId)
            ? patientId
            : throw new InvalidOperationException("The trusted surgery patient scope is incomplete.");
}

internal sealed record SurgeryAssistantTeamMember(string UserId, string Role);

internal sealed record SurgeryAssistantProposal(
    Guid Id,
    Guid PatientId,
    string CatalogId,
    string Name,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContact> Contacts,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies,
    int? DurationMinutes = null,
    string? Location = null,
    IReadOnlyList<SurgeryAssistantTeamMember>? Team = null);

internal sealed record SurgeryAssistantPreparationResult(SurgeryAssistantProposal Proposal);
internal sealed record SurgeryAssistantListResult(IReadOnlyList<SurgeryReadModel> Surgeries);
internal sealed record SurgeryAssistantTypesResult(
    [property: JsonPropertyName("types")] IReadOnlyList<SurgeryCatalog> Types);

[JsonSerializable(typeof(SurgeryAssistantProposal))]
[JsonSerializable(typeof(SurgeryAssistantPreparationResult))]
[JsonSerializable(typeof(SurgeryAssistantListResult))]
[JsonSerializable(typeof(SurgeryAssistantTypesResult))]
[JsonSerializable(typeof(SurgeryAssistantTeamMember))]
[JsonSerializable(typeof(SurgeryCatalog))]
[JsonSerializable(typeof(SurgeryReadModel))]
internal sealed partial class SurgeryAssistantJsonSerializerContext : JsonSerializerContext;
