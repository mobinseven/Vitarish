using FluentAssertions;
using Vitarish.Clinic.Agents.Tests.Live;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests;

/// <summary>The Clinic domain model stays true to the tools the Clinic assistants register and expose (ADR-0023).</summary>
public sealed class DomainModelTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Every_tool_the_domain_model_names_is_a_registered_clinic_tool()
    {
        HashSet<string> registered = [.. LiveOperationsAssistant.RealCapabilities().SelectMany(capability => capability.Tools).Select(tool => tool.Definition.Name)];

        ClinicAssistantDomainModel.Create().ToolNames.Should().BeSubsetOf(registered);
    }

    [Fact]
    public void The_patient_assistant_reads_procedures_with_patient_page_tools()
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Patient.Compose(Now);
        string instructions = manifest.Definition.Instructions;

        instructions.Should().Contain("Domain model: Clinic");
        instructions.Should().Contain($"- To list with progress, current step and next step: {ClinicPresentableToolNames.ListPatientProcedureProgress}");
        instructions.Should().Contain($"- To read with its notes, files and reminder count: {ClinicPresentableToolNames.GetPatientProcedureStep}");
        instructions.Should().NotContain(ClinicPresentableToolNames.ListOperationsPatientProcedures);
        manifest.Tools.Select(tool => tool.Definition.Name).Should().Contain(ClinicPresentableToolNames.ListPatientProcedureProgress);
    }

    [Fact]
    public void The_operations_assistant_reads_procedures_with_practice_tools_and_says_what_it_cannot_do()
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Compose(Now);
        string instructions = manifest.Definition.Instructions;

        instructions.Should().Contain($"- To list with progress, current step and next step: {ClinicPresentableToolNames.ListOperationsPatientProcedures}");
        instructions.Should().Contain("- To add a documented visit: not available in this conversation; the user can do this from the patient's page");
        manifest.Tools.Select(tool => tool.Definition.Name).Should().Contain(ClinicPresentableToolNames.ListOperationsPatientProcedures);
    }

    [Fact]
    public void Nothing_the_model_reads_about_procedures_names_the_workflow_module()
    {
        foreach (ComposedAgentManifest<AssistantCompositionContext> manifest in new[] { LiveOperationsAssistant.Patient.Compose(Now), LiveOperationsAssistant.Compose(Now) })
        {
            IEnumerable<AgentToolDefinition> procedureTools = manifest.Tools.Select(tool => tool.Definition)
                .Where(definition => definition.Name.Contains("procedure", StringComparison.Ordinal) || definition.Name.Contains("step", StringComparison.Ordinal));
            procedureTools.Should().NotBeEmpty();
            procedureTools.Should().OnlyContain(definition => !definition.Description.Contains("SEA", StringComparison.Ordinal));
            AgentDomainModelInstructions.Render(ClinicAssistantDomainModel.Create(), manifest.Tools.Select(tool => tool.Definition.Name))
                .Should().NotContain("SEA");
        }
    }
}
