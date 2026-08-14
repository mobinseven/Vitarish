using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant.Capabilities;

public sealed class AssistantToolDispatcher(
    AssistantCapabilityRegistry capabilityRegistry,
    ILogger<AssistantToolDispatcher>? logger = null)
{
    public IReadOnlyCollection<IAgentTool> CreateTools(
        AssistantContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<IAssistantCapability> capabilities)
    {
        IReadOnlyCollection<IAgentTool> tools = capabilities.SelectMany(capability => capability.Tools)
            .Where(tool => capabilityRegistry.ResolveTool(tool.Definition.Name) is not { } policy || policy.Authorizes(context))
            .ToArray();
        IReadOnlyDictionary<string, string> toolCapabilities = capabilities
            .SelectMany(capability => capability.Tools.Select(tool => (ToolName: tool.Definition.Name, CapabilityName: capability.Name)))
            .ToDictionary(item => item.ToolName, item => item.CapabilityName, StringComparer.Ordinal);
        return CreateGuardedTools(context, executionContext, tools, toolCapabilities);
    }

    public IReadOnlyCollection<IAgentTool> CreateGuardedTools(
        AssistantContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<IAgentTool> tools,
        IReadOnlyDictionary<string, string> toolCapabilities) =>
        CreateGuardedTools(
            context,
            executionContext,
            tools.Select(tool =>
            {
                if (!toolCapabilities.TryGetValue(tool.Definition.Name, out string? capability))
                    throw new InvalidOperationException($"The clinic assistant tool '{tool.Definition.Name}' has no registered capability.");
                return (capability, tool);
            }).ToArray());

    private IReadOnlyCollection<IAgentTool> CreateGuardedTools(
        AssistantContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<(string Capability, IAgentTool Tool)> tools) =>
        tools.Select(item => (IAgentTool)new GuardedTool(item.Tool, item.Capability, context, executionContext.ExecutionId, capabilityRegistry, logger))
            .ToArray();

    private sealed class GuardedTool(
        IAgentTool handler,
        string capability,
        AssistantContext trustedContext,
        string agentRunId,
        AssistantCapabilityRegistry capabilityRegistry,
        ILogger? logger) : IAgentTool, IAssistantPreparationTool
    {
        public AgentToolDefinition Definition => handler.Definition;

        public AssistantPreparedAction? GetPreparedAction(AgentToolResult result) =>
            (handler as IAssistantPreparationTool)?.GetPreparedAction(result);

        public async Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(call.Name, Definition.Name, StringComparison.Ordinal))
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "tool_name_mismatch", context.ExecutionId, call.Id);
                throw new InvalidOperationException("The requested clinic assistant tool is not registered.");
            }

            if (!IsCorrelated(context, trustedContext, agentRunId))
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "execution_scope_mismatch", context.ExecutionId, call.Id);
                throw new InvalidOperationException("The clinic assistant tool invocation is not correlated with the current run.");
            }

            if (trustedContext.SubjectKind == "practice" &&
                !AssistantOperationsScope.IsAuthorizedRole(trustedContext.Role))
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "operations_scope_denied", context.ExecutionId, call.Id);
                throw new UnauthorizedAccessException("The operations agent requires a current practice-owner scope.");
            }

            AssistantActionPolicy? actionPolicy = capabilityRegistry.ResolveTool(call.Name);
            if (actionPolicy is not null && !actionPolicy.Authorizes(trustedContext))
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "action_unauthorized", context.ExecutionId, call.Id);
                throw new UnauthorizedAccessException("The clinic assistant action is not authorized for the current scope.");
            }
            if (actionPolicy is { ExecutionMode: AssistantActionExecutionMode.PrepareAndConfirm } &&
                handler is not IAssistantPreparationTool)
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "action_requires_preparation", context.ExecutionId, call.Id);
                throw new InvalidOperationException("The clinic assistant action must be prepared before confirmation.");
            }
            if (actionPolicy is { ExecutionMode: not AssistantActionExecutionMode.DirectAllowlisted } &&
                handler is not IAssistantPreparationTool)
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "action_not_allowlisted", context.ExecutionId, call.Id);
                throw new InvalidOperationException("The clinic assistant action is not directly executable.");
            }

            try
            {
                ValidateArguments(call.ArgumentsJson, Definition.ParametersJsonSchema);
            }
            catch
            {
                if (logger is not null) AssistantTelemetry.ToolRejected(logger, call.Name, capability, "arguments_invalid", context.ExecutionId, call.Id);
                throw;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                AgentToolResult result = await handler.ExecuteAsync(call, context, cancellationToken);
                if (logger is not null)
                {
                    AssistantTelemetry.ToolCompleted(logger, call.Name, capability,
                        result.IsError ? "error" : "succeeded", stopwatch.ElapsedMilliseconds, context.ExecutionId, call.Id);
                }
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                if (logger is not null)
                {
                    AssistantTelemetry.ToolFailed(logger, call.Name, capability, stopwatch.ElapsedMilliseconds, context.ExecutionId, call.Id);
                }
                throw;
            }
        }

        private static bool IsCorrelated(AgentExecutionContext execution, AssistantContext trusted, string expectedRunId) =>
            string.Equals(execution.ExecutionId, expectedRunId, StringComparison.Ordinal) &&
            execution.Metadata is not null &&
            execution.Metadata.TryGetValue("practiceId", out string? practiceId) && practiceId == trusted.PracticeId &&
            execution.Metadata.TryGetValue("patientId", out string? patientId) && patientId == trusted.PatientId.ToString("D") &&
            (!execution.Metadata.TryGetValue("subjectKind", out string? subjectKind)
                ? trusted.SubjectKind == "patient"
                : subjectKind == trusted.SubjectKind) &&
            execution.Metadata.TryGetValue("authenticatedUserId", out string? userId) && userId == trusted.AuthenticatedUserId;

        private static void ValidateArguments(string argumentsJson, string schemaJson)
        {
            using JsonDocument arguments = JsonDocument.Parse(argumentsJson);
            using JsonDocument schema = JsonDocument.Parse(schemaJson);
            if (schema.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("The clinic assistant tool schema must be an object.");
            }

            ValidateValue(arguments.RootElement, schema.RootElement);
        }

        private static void ValidateValue(JsonElement value, JsonElement schema)
        {
            if (schema.TryGetProperty("type", out JsonElement type) && !MatchesType(value, type.GetString()))
            {
                throw new JsonException("Clinic assistant tool arguments do not match the declared type.");
            }

            if (schema.TryGetProperty("enum", out JsonElement allowed) &&
                !allowed.EnumerateArray().Any(item => JsonElement.DeepEquals(item, value)))
            {
                throw new JsonException("Clinic assistant tool arguments contain a value outside the declared enum.");
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
                        throw new JsonException("Clinic assistant tool arguments are missing a required property.");
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
                    throw new JsonException("Clinic assistant tool arguments contain an unknown property.");
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
            _ => throw new JsonException("The clinic assistant tool schema declares an unsupported type.")
        };
    }
}
