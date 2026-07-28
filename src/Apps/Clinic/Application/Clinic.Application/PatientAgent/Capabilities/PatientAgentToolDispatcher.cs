using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities;

public sealed class PatientAgentToolDispatcher(PatientAgentApprovalPolicy approvalPolicy)
{
    public IReadOnlyCollection<IAgentTool> CreateTools(
        PatientAgentContext context,
        IReadOnlyCollection<IPatientAgentCapability> capabilities) =>
        capabilities.SelectMany(capability => capability.Tools)
            .Select(tool => (IAgentTool)new GuardedTool(tool, context, approvalPolicy))
            .ToArray();

    private sealed class GuardedTool(
        IAgentTool handler,
        PatientAgentContext trustedContext,
        PatientAgentApprovalPolicy approvalPolicy) : IAgentTool
    {
        public AgentToolDefinition Definition => handler.Definition;

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(call.Name, Definition.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The requested patient agent tool is not registered.");
            }

            if (!IsCorrelated(context, trustedContext))
            {
                throw new InvalidOperationException("The patient agent tool invocation is not correlated with the current run.");
            }

            if (approvalPolicy.RequiresApproval(call.Name))
            {
                throw new InvalidOperationException("The patient agent tool requires approval before invocation.");
            }

            ValidateArguments(call.ArgumentsJson, Definition.ParametersJsonSchema);
            return handler.ExecuteAsync(call, context, cancellationToken);
        }

        private static bool IsCorrelated(AgentExecutionContext execution, PatientAgentContext trusted) =>
            string.Equals(execution.ExecutionId, trusted.ConversationId, StringComparison.Ordinal) &&
            execution.Metadata is not null &&
            execution.Metadata.TryGetValue("practiceId", out string? practiceId) && practiceId == trusted.PracticeId &&
            execution.Metadata.TryGetValue("patientId", out string? patientId) && patientId == trusted.PatientId.ToString("D") &&
            execution.Metadata.TryGetValue("authenticatedUserId", out string? userId) && userId == trusted.AuthenticatedUserId;

        private static void ValidateArguments(string argumentsJson, string schemaJson)
        {
            using JsonDocument arguments = JsonDocument.Parse(argumentsJson);
            using JsonDocument schema = JsonDocument.Parse(schemaJson);
            if (arguments.RootElement.ValueKind != JsonValueKind.Object || schema.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Patient agent tool arguments must be an object.");
            }

            HashSet<string> argumentNames = arguments.RootElement.EnumerateObject()
                .Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            if (schema.RootElement.TryGetProperty("required", out JsonElement required))
            {
                foreach (JsonElement property in required.EnumerateArray())
                {
                    if (property.GetString() is not { } name || !argumentNames.Contains(name))
                    {
                        throw new JsonException("Patient agent tool arguments are missing a required property.");
                    }
                }
            }

            if (schema.RootElement.TryGetProperty("additionalProperties", out JsonElement additionalProperties) &&
                additionalProperties.ValueKind == JsonValueKind.False)
            {
                HashSet<string> definedNames = schema.RootElement.TryGetProperty("properties", out JsonElement properties)
                    ? properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                    : [];
                if (!argumentNames.IsSubsetOf(definedNames))
                {
                    throw new JsonException("Patient agent tool arguments contain an unknown property.");
                }
            }
        }
    }
}
