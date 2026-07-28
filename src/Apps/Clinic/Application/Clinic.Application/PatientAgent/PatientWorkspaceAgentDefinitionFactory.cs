using Vitarish.Intelligence.Application.Agents;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class PatientWorkspaceAgentDefinitionFactory(
    PatientAgentCapabilityRegistry capabilityRegistry,
    PatientAgentToolDispatcher toolDispatcher,
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

        IReadOnlyCollection<IPatientAgentCapability> enabled = capabilityRegistry.GetAvailable(context);
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
        if (!string.IsNullOrWhiteSpace(context.PatientNationalId))
        {
            metadata["patientNationalId"] = context.PatientNationalId;
        }
        AgentExecutionContext executionContext = new(Guid.NewGuid().ToString("N"), metadata);
        PatientAgentExecutionCollector collector = new();
        IReadOnlyCollection<IAgentTool> tools = toolDispatcher.CreateTools(context, executionContext, enabled, collector);

        return new PatientWorkspaceAgentDefinition(
            new AgentDefinition(
                options.Model,
                instructionComposer.Compose(context, enabled),
                new AgentResultDefinition("patient_workspace_result", PatientWorkspaceAgentDefinition.ResultSchema)),
            tools,
            enabledActions,
            collector,
            executionContext,
            new AgentExecutionLimits());
    }

}
