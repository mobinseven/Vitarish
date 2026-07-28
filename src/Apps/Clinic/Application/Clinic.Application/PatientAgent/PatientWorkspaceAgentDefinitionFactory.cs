using Vitarish.Intelligence.Application.Agents;
using System.Text.Json;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class PatientWorkspaceAgentDefinitionFactory(
    IEnumerable<IPatientAgentCapability> capabilities,
    PatientAgentInstructionComposer instructionComposer,
    PatientWorkspaceAgentOptions options)
{
    public PatientWorkspaceAgentDefinition Create(PatientAgentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException("A configured patient workspace agent model is required.");
        }

        if (context.PatientId == Guid.Empty)
        {
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));
        }

        IPatientAgentCapability[] enabled = capabilities
            .Where(capability => capability.RequiredPermissions.All(context.Permissions.Contains))
            .ToArray();
        IAgentTool[] tools = enabled.SelectMany(capability => capability.ContextTools).ToArray();
        ValidateTools(tools);
        Dictionary<string, IReadOnlySet<string>> enabledActions = enabled.ToDictionary(
            capability => capability.Name,
            capability => (IReadOnlySet<string>)capability.ActionTypes.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);

        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["role"] = context.Role,
            ["locale"] = context.Locale,
            ["timeZone"] = context.TimeZone,
            ["conversationId"] = context.ConversationId
        };

        return new PatientWorkspaceAgentDefinition(
            new AgentDefinition(
                options.Model,
                instructionComposer.Compose(context, enabled),
                new AgentResultDefinition("patient_workspace_result", PatientWorkspaceAgentDefinition.ResultSchema)),
            tools,
            enabledActions,
            new AgentExecutionContext(context.ConversationId, metadata),
            new AgentExecutionLimits());
    }

    private static void ValidateTools(IReadOnlyCollection<IAgentTool> tools)
    {
        if (tools.Select(tool => tool.Definition.Name).Distinct(StringComparer.Ordinal).Count() != tools.Count)
        {
            throw new InvalidOperationException("Patient agent tool names must be unique.");
        }

        foreach (IAgentTool tool in tools)
        {
            using JsonDocument schema = JsonDocument.Parse(tool.Definition.ParametersJsonSchema);
            if (ContainsPatientIdProperty(schema.RootElement))
            {
                throw new InvalidOperationException("Patient agent tools must use the route-scoped patient and cannot accept patientId.");
            }
        }
    }

    private static bool ContainsPatientIdProperty(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, "patientId", StringComparison.OrdinalIgnoreCase) ||
                    ContainsPatientIdProperty(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().Any(ContainsPatientIdProperty);
        }

        return false;
    }
}
