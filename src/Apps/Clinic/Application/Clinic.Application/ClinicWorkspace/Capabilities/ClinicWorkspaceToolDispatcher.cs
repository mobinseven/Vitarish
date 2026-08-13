using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Clinic.Application.ClinicWorkspace.Policies;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public sealed class ClinicWorkspaceToolDispatcher(
    ClinicWorkspaceApprovalPolicy approvalPolicy,
    IEnumerable<IIntelligenceAgentTool> intelligenceTools,
    ILogger<ClinicWorkspaceToolDispatcher>? logger = null)
{
    private readonly IReadOnlyCollection<IIntelligenceAgentTool> _intelligenceTools = [.. intelligenceTools];

    public IReadOnlyCollection<IAgentTool> CreateTools(
        ClinicWorkspaceContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<IClinicWorkspaceCapability> capabilities)
    {
        IReadOnlyCollection<IAgentTool> tools = capabilities.SelectMany(capability => capability.Tools)
            .Concat(_intelligenceTools.Cast<IAgentTool>())
            .ToArray();
        IReadOnlyDictionary<string, string> toolCapabilities = capabilities
            .SelectMany(capability => capability.Tools.Select(tool => (ToolName: tool.Definition.Name, CapabilityName: capability.Name)))
            .Concat(_intelligenceTools.Select(tool => (ToolName: tool.Definition.Name, CapabilityName: "Intelligence")))
            .ToDictionary(item => item.ToolName, item => item.CapabilityName, StringComparer.Ordinal);
        return CreateGuardedTools(context, executionContext, tools, toolCapabilities);
    }

    public IReadOnlyCollection<IAgentTool> CreateGuardedTools(
        ClinicWorkspaceContext context,
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
        ClinicWorkspaceContext context,
        AgentExecutionContext executionContext,
        IReadOnlyCollection<(string Capability, IAgentTool Tool)> tools) =>
        tools.Select(item => (IAgentTool)new GuardedTool(item.Tool, item.Capability, context, executionContext.ExecutionId, approvalPolicy, logger))
            .ToArray();

    private sealed class GuardedTool(
        IAgentTool handler,
        string capability,
        ClinicWorkspaceContext trustedContext,
        string agentRunId,
        ClinicWorkspaceApprovalPolicy approvalPolicy,
        ILogger? logger) : IAgentTool, IClinicWorkspacePreparationTool
    {
        public AgentToolDefinition Definition => handler.Definition;

        public ClinicWorkspacePreparedAction? GetPreparedAction(AgentToolResult result) =>
            (handler as IClinicWorkspacePreparationTool)?.GetPreparedAction(result);

        public async Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(call.Name, Definition.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The requested clinic assistant tool is not registered.");
            }

            if (!IsCorrelated(context, trustedContext, agentRunId))
            {
                throw new InvalidOperationException("The clinic assistant tool invocation is not correlated with the current run.");
            }

            if (trustedContext.SubjectKind == "practice" &&
                !ClinicWorkspaceOperationsScope.IsAuthorizedRole(trustedContext.Role))
            {
                throw new UnauthorizedAccessException("The operations agent requires a current practice-owner scope.");
            }

            if (approvalPolicy.RequiresApproval(call.Name))
            {
                throw new InvalidOperationException("The clinic assistant tool requires approval before invocation.");
            }

            ValidateArguments(call.ArgumentsJson, Definition.ParametersJsonSchema);
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                AgentToolResult result = await handler.ExecuteAsync(call, context, cancellationToken);
                if (logger is not null)
                {
                    ClinicWorkspaceTelemetry.ToolCompleted(logger, call.Name, capability,
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
                    ClinicWorkspaceTelemetry.ToolFailed(logger, call.Name, capability, stopwatch.ElapsedMilliseconds, context.ExecutionId, call.Id);
                }
                throw;
            }
        }

        private static bool IsCorrelated(AgentExecutionContext execution, ClinicWorkspaceContext trusted, string expectedRunId) =>
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
