using Vitarish.Intelligence.Application.Agents;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class PatientWorkspaceAgentDefinitionFactory(
    PatientAgentCapabilityRegistry capabilityRegistry,
    PatientAgentToolDispatcher toolDispatcher,
    PatientAgentInstructionComposer instructionComposer,
    PatientWorkspaceAgentOptions options,
    ILogger<PatientWorkspaceAgentDefinitionFactory>? logger = null,
    TimeProvider? timeProvider = null)
{
    public PatientWorkspaceAgentDefinition Create(PatientAgentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(options.Model))
        {
            throw new InvalidOperationException("A configured patient workspace agent model is required.");
        }

        if (context.SubjectKind == "patient" && context.PatientId == Guid.Empty)
        {
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));
        }

        IReadOnlyCollection<IPatientAgentCapability> enabled = capabilityRegistry.GetAvailable(context);
        logger?.LogInformation(
            "Patient agent capabilities selected: {Capabilities}.",
            string.Join(", ", enabled.Select(capability => capability.Name).OrderBy(name => name, StringComparer.Ordinal)));
        Dictionary<string, IReadOnlySet<string>> enabledActions = enabled.ToDictionary(
            capability => capability.Name,
            capability => (IReadOnlySet<string>)capability.ActionTypes.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        DateTimeOffset currentUtc = (timeProvider ?? TimeProvider.System).GetUtcNow();
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);

        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["permissions"] = JsonSerializer.Serialize(context.Permissions),
            ["role"] = context.Role,
            ["locale"] = context.Locale,
            ["calendar"] = context.Calendar,
            ["timeZone"] = context.TimeZone,
            ["currentUtc"] = currentUtc.ToString("O"),
            ["currentLocal"] = currentLocal.ToString("O"),
            ["conversationId"] = context.ConversationId
        };
        metadata["subjectKind"] = context.SubjectKind;
        if (!string.IsNullOrWhiteSpace(context.PatientNationalId))
        {
            metadata["patientNationalId"] = context.PatientNationalId;
        }
        AgentExecutionContext executionContext = new(Guid.NewGuid().ToString("N"), metadata);
        PatientAgentExecutionCollector collector = new();
        IReadOnlyCollection<IAgentTool> tools = toolDispatcher.CreateTools(context, executionContext, enabled, collector);
        Dictionary<string, string> toolCapabilities = enabled
            .SelectMany(capability => capability.Tools.Select(tool =>
                (ToolName: tool.Definition.Name, Capability: capability.Name)))
            .ToDictionary(item => item.ToolName, item => item.Capability, StringComparer.Ordinal);
        foreach (IAgentTool tool in tools)
        {
            toolCapabilities.TryAdd(tool.Definition.Name, "Intelligence");
        }
        logger?.LogInformation(
            "Patient agent tools created: {ToolCount} tools: {ToolNames}.",
            tools.Count,
            string.Join(", ", tools.Select(tool => tool.Definition.Name).OrderBy(name => name, StringComparer.Ordinal)));

        return new PatientWorkspaceAgentDefinition(
            new AgentDefinition(
                options.Model,
                instructionComposer.Compose(context, enabled, currentUtc),
                new AgentResultDefinition("patient_workspace_result", PatientWorkspaceAgentDefinition.ResultSchema)),
            tools,
            enabledActions,
            collector,
            executionContext,
            new AgentExecutionLimits(),
            toolCapabilities);
    }

}
