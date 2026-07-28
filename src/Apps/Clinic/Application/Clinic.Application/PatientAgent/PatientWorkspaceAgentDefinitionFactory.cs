using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class PatientWorkspaceAgentDefinitionFactory(
    IEnumerable<IPatientAgentCapability> capabilities,
    PatientAgentInstructionComposer instructionComposer)
{
    public PatientWorkspaceAgentDefinition Create(PatientAgentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.PatientId == Guid.Empty)
        {
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));
        }

        IPatientAgentCapability[] enabled = capabilities
            .Where(capability => capability.RequiredPermissions.All(context.Permissions.Contains))
            .ToArray();
        IAgentTool[] tools = enabled.SelectMany(capability => capability.ContextTools).ToArray();

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
                "patient-workspace",
                instructionComposer.Compose(context, enabled),
                new AgentResultDefinition("patient_workspace_result", PatientWorkspaceAgentDefinition.ResultSchema)),
            tools,
            new AgentExecutionContext(context.ConversationId, metadata),
            new AgentExecutionLimits());
    }
}
