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
}

public sealed class PatientSurgeryAgentCapability(
    ListPatientSurgeriesAssistantTool list,
    PreparePatientSurgeryTool prepare) : AssistantCapability([list, prepare], [
        AssistantActionPolicies.Prepare(
            SurgeryAssistantActions.Create,
            SurgeryAssistantActions.Workflow,
            ["ClinicalEvents.Create"],
            toolNames: ["prepare_patient_surgery"],
            retryClassification: AssistantActionRetryClassification.Idempotent)
    ])
{
    public override string Name => SurgeryAssistantActions.Capability;
    public override string Description => "Read current-patient surgeries and prepare a surgery plan for explicit clinician confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read", "ClinicalEvents.Create" };
    public override string GetInstructions() =>
        "Use only the server-supplied patient scope. Confirm the full surgery plan, contacts, and reminder policy with the clinician before preparing it; never create it during the model run.";
}

public sealed class ListPatientSurgeriesAssistantTool(ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        "list_patient_surgeries",
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

public sealed class PreparePatientSurgeryTool : IAgentTool, IAssistantPreparationTool
{
    private const string ToolName = "prepare_patient_surgery";
    private readonly ISurgeryToolchain _surgeries;

    public PreparePatientSurgeryTool(ISurgeryToolchain surgeries) => _surgeries = surgeries;

    public AgentToolDefinition Definition { get; } = new(
        ToolName,
        "Prepare a current-patient surgery plan for explicit clinician confirmation; this does not create it.",
        """{"type":"object","properties":{"catalogId":{"type":"string"},"name":{"type":"string"},"scheduledAt":{"type":"string"},"tools":{"type":"array","items":{"type":"string"}},"personsToNotify":{"type":"array","items":{"type":"string"}},"contacts":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"destination":{"type":"string"},"channel":{"type":"string","enum":["email","sms"]}},"required":["name","destination","channel"],"additionalProperties":false}},"reminderPolicies":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"minutesBefore":{"type":"integer","minimum":0},"subject":{"type":"string"},"textBody":{"type":"string"}},"required":["name","minutesBefore","subject","textBody"],"additionalProperties":false}}},"required":["catalogId","name","scheduledAt"],"additionalProperties":false}""");

    public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement arguments = document.RootElement;
        string catalogId = arguments.GetProperty("catalogId").GetString() ?? string.Empty;
        if (!_surgeries.GetCatalogs().Any(catalog => string.Equals(catalog.Id, catalogId, StringComparison.Ordinal)))
        {
            throw new ArgumentException("The surgery catalog does not exist.", nameof(catalogId));
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
            Policies(arguments));
        return Task.FromResult(new AgentToolResult(
            call.Id,
            JsonSerializer.Serialize(
                new SurgeryAssistantPreparationResult(proposal),
                SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantPreparationResult)));
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
                $"Create surgery '{proposal.Name}' at {proposal.ScheduledAt:O}.")
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
}

internal static class SurgeryAssistantScope
{
    public static Guid PatientId(AgentExecutionContext context) =>
        context.Metadata is not null && context.Metadata.TryGetValue("patientId", out string? value) && Guid.TryParse(value, out Guid patientId)
            ? patientId
            : throw new InvalidOperationException("The trusted surgery patient scope is incomplete.");
}

internal sealed record SurgeryAssistantProposal(
    Guid Id,
    Guid PatientId,
    string CatalogId,
    string Name,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContact> Contacts,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies);

internal sealed record SurgeryAssistantPreparationResult(SurgeryAssistantProposal Proposal);
internal sealed record SurgeryAssistantListResult(IReadOnlyList<SurgeryReadModel> Surgeries);

[JsonSerializable(typeof(SurgeryAssistantProposal))]
[JsonSerializable(typeof(SurgeryAssistantPreparationResult))]
[JsonSerializable(typeof(SurgeryAssistantListResult))]
[JsonSerializable(typeof(SurgeryReadModel))]
internal sealed partial class SurgeryAssistantJsonSerializerContext : JsonSerializerContext;
