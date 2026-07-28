using System.Text.Json;
using Vitarish.Clinic.Application.PatientAgent.Policies;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent.Capabilities;

public sealed class PatientAgentToolDispatcher(PatientAgentApprovalPolicy approvalPolicy)
{
    public IReadOnlyCollection<IAgentTool> CreateTools(
        PatientAgentContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<IPatientAgentCapability> capabilities) =>
        CreateTools(context, executionContext, capabilities, new PatientAgentExecutionCollector());

    public IReadOnlyCollection<IAgentTool> CreateTools(
        PatientAgentContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<IPatientAgentCapability> capabilities,
        PatientAgentExecutionCollector collector) =>
        capabilities.SelectMany(capability => capability.Tools.Select(tool => (capability.Name, Tool: tool)))
            .Select(item => (IAgentTool)new GuardedTool(item.Tool, item.Name, context, executionContext.ExecutionId, approvalPolicy, collector))
            .ToArray();

    private sealed class GuardedTool(
        IAgentTool handler,
        string capability,
        PatientAgentContext trustedContext,
        string agentRunId,
        PatientAgentApprovalPolicy approvalPolicy,
        PatientAgentExecutionCollector collector) : IAgentTool
    {
        public AgentToolDefinition Definition => handler.Definition;

        public async Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(call.Name, Definition.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The requested patient agent tool is not registered.");
            }

            if (!IsCorrelated(context, trustedContext, agentRunId))
            {
                throw new InvalidOperationException("The patient agent tool invocation is not correlated with the current run.");
            }

            if (approvalPolicy.RequiresApproval(call.Name))
            {
                throw new InvalidOperationException("The patient agent tool requires approval before invocation.");
            }

            ValidateArguments(call.ArgumentsJson, Definition.ParametersJsonSchema);
            AgentToolResult result = await handler.ExecuteAsync(call, context, cancellationToken);
            collector.Record(capability, call, context, result);
            return result;
        }

        private static bool IsCorrelated(AgentExecutionContext execution, PatientAgentContext trusted, string expectedRunId) =>
            string.Equals(execution.ExecutionId, expectedRunId, StringComparison.Ordinal) &&
            execution.Metadata is not null &&
            execution.Metadata.TryGetValue("practiceId", out string? practiceId) && practiceId == trusted.PracticeId &&
            execution.Metadata.TryGetValue("patientId", out string? patientId) && patientId == trusted.PatientId.ToString("D") &&
            execution.Metadata.TryGetValue("authenticatedUserId", out string? userId) && userId == trusted.AuthenticatedUserId;

        private static void ValidateArguments(string argumentsJson, string schemaJson)
        {
            using JsonDocument arguments = JsonDocument.Parse(argumentsJson);
            using JsonDocument schema = JsonDocument.Parse(schemaJson);
            if (schema.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("The patient agent tool schema must be an object.");
            }

            ValidateValue(arguments.RootElement, schema.RootElement);
        }

        private static void ValidateValue(JsonElement value, JsonElement schema)
        {
            if (schema.TryGetProperty("type", out JsonElement type) && !MatchesType(value, type.GetString()))
            {
                throw new JsonException("Patient agent tool arguments do not match the declared type.");
            }

            if (schema.TryGetProperty("enum", out JsonElement allowed) &&
                !allowed.EnumerateArray().Any(item => JsonElement.DeepEquals(item, value)))
            {
                throw new JsonException("Patient agent tool arguments contain a value outside the declared enum.");
            }

            if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out JsonElement itemSchema))
            {
                foreach (JsonElement item in value.EnumerateArray())
                {
                    ValidateValue(item, itemSchema);
                }
            }

            if (value.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            HashSet<string> argumentNames = value.EnumerateObject()
                .Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
            if (schema.TryGetProperty("required", out JsonElement required))
            {
                foreach (JsonElement property in required.EnumerateArray())
                {
                    if (property.GetString() is not { } name || !argumentNames.Contains(name))
                    {
                        throw new JsonException("Patient agent tool arguments are missing a required property.");
                    }
                }
            }

            if (schema.TryGetProperty("additionalProperties", out JsonElement additionalProperties) &&
                additionalProperties.ValueKind == JsonValueKind.False)
            {
                HashSet<string> definedNames = schema.TryGetProperty("properties", out JsonElement properties)
                    ? properties.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                    : [];
                if (!argumentNames.IsSubsetOf(definedNames))
                {
                    throw new JsonException("Patient agent tool arguments contain an unknown property.");
                }
            }

            if (schema.TryGetProperty("properties", out JsonElement propertySchemas))
            {
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    if (propertySchemas.TryGetProperty(property.Name, out JsonElement propertySchema))
                    {
                        ValidateValue(property.Value, propertySchema);
                    }
                }
            }
        }

        private static bool MatchesType(JsonElement value, string? type) => type switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "number" => value.ValueKind == JsonValueKind.Number,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => value.ValueKind == JsonValueKind.Null,
            null => true,
            _ => throw new JsonException("The patient agent tool schema declares an unsupported type.")
        };
    }
}
