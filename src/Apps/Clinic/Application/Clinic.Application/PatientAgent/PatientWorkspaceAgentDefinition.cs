using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientWorkspaceAgentDefinition(
    AgentDefinition Definition,
    IReadOnlyCollection<IAgentTool> Tools,
    IReadOnlyDictionary<string, IReadOnlySet<string>> EnabledActions,
    PatientAgentExecutionCollector ExecutionCollector,
    AgentExecutionContext ExecutionContext,
    AgentExecutionLimits Limits)
{
    public const string ResultSchema = """
        {
          "type":"object",
          "additionalProperties":false,
          "required":["messages","actions"],
          "properties":{
            "messages":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["kind","content"],"properties":{"kind":{"type":"string","enum":["assistantMessage","informationalToolResult","missingInformationRequest","failure"]},"content":{"type":"string"}}}},
            "actions":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["capability","actionType","summary"],"properties":{"capability":{"type":"string"},"actionType":{"type":"string"},"summary":{"type":"string"}}}}
          }
        }
        """;
}
