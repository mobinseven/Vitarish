using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientWorkspaceAgentDefinition(
    AgentDefinition Definition,
    IReadOnlyCollection<IAgentTool> Tools,
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
            "actions":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["id","capability","actionType","status","arguments","summary"],"properties":{"id":{"type":"string"},"capability":{"type":"string"},"actionType":{"type":"string"},"status":{"type":"string","enum":["proposed","completed"]},"arguments":{"type":"object"},"summary":{"type":"string"}}}}
          }
        }
        """;
}
