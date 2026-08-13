using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public interface IPatientWorkspaceAgentComposer
{
    PatientWorkspaceAgentDefinition Compose(
        PatientAgentContext context,
        AgentExecutionContext? executionContext = null);
}
