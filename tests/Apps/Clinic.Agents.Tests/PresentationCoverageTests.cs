using FluentAssertions;
using Vitarish.Clinic.Agents.Tests.Live;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Contents.Agents;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests;

public sealed class PresentationCoverageTests
{
    private static readonly IReadOnlySet<string> MutationOrPresentationTools = new HashSet<string>(StringComparer.Ordinal)
    {
        ContentsCreateDraftEntryAgentTool.Name,
        ContentsSaveDraftAgentTool.Name,
        PresentSurfaceTool.Name
    };

    [Fact]
    public void Every_read_tool_of_the_patient_assistant_is_presented_or_exempt()
    {
        IReadOnlyList<string> readTools = ReadTools(AssistantCapabilityKeys.Patient);

        readTools.Should().NotBeEmpty();
        Uncovered(readTools, AssistantSubjects.PatientAgentKeys).Should().BeEmpty();
    }

    [Fact]
    public void Every_read_tool_of_the_operations_assistant_is_presented_or_exempt()
    {
        IReadOnlyList<string> readTools = ReadTools(AssistantCapabilityKeys.OperationsProfile);

        readTools.Should().NotBeEmpty();
        Uncovered(readTools, AssistantSubjects.OperationsAgentKeys).Should().BeEmpty();
    }

    [Fact]
    public void No_tool_is_both_presented_and_exempt()
    {
        string[] presented = ClinicAssistantPresentations.All().Select(presentation => presentation.ToolName).ToArray();

        presented.Intersect(ClinicAssistantPresentations.Exempt.Keys, StringComparer.Ordinal).Should().BeEmpty();
    }

    [Fact]
    public void Presentations_and_exemptions_name_only_tools_the_assistants_expose()
    {
        HashSet<string> readTools = [.. ReadTools(AssistantCapabilityKeys.Patient), .. ReadTools(AssistantCapabilityKeys.OperationsProfile)];

        ClinicAssistantPresentations.All().Select(presentation => presentation.ToolName)
            .Concat(ClinicAssistantPresentations.Exempt.Keys)
            .Where(name => !readTools.Contains(name))
            .Should().BeEmpty();
    }

    [Fact]
    public void Each_presentation_is_registered_only_for_agents_that_expose_the_tool()
    {
        IReadOnlyDictionary<string, HashSet<string>> toolsByAgent = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            [AssistantSubjects.PatientAgentKey] = [.. ReadTools(AssistantCapabilityKeys.Patient)],
            [AssistantSubjects.OperationsAgentKey] = [.. ReadTools(AssistantCapabilityKeys.OperationsProfile)]
        };

        ClinicAssistantPresentations.All()
            .SelectMany(presentation => presentation.AgentKeys.Select(key => (key, presentation.ToolName)))
            .Where(pair => pair.key is var key && toolsByAgent.TryGetValue(key, out HashSet<string>? tools) && !tools.Contains(pair.ToolName))
            .Should().BeEmpty();
    }

    private static string[] Uncovered(IReadOnlyList<string> readTools, IReadOnlyCollection<string> agentKeys)
    {
        AgentToolPresentationCatalog catalog = new(ClinicAssistantPresentations.All());
        return [.. readTools.Where(name =>
            catalog.Find(agentKeys.First(), name) is null && !ClinicAssistantPresentations.Exempt.ContainsKey(name))];
    }

    // A read tool is any tool of the profile's real capabilities that is not a preparation tool, a direct mutation
    // named by an action policy, a Contents draft write, or the presentation tool itself.
    private static IReadOnlyList<string> ReadTools(IEnumerable<string> capabilityKeys)
    {
        IAssistantCapability[] capabilities = [.. LiveOperationsAssistant.RealCapabilities()
            .Where(capability => capabilityKeys.Contains(capability.Name, StringComparer.Ordinal))];
        HashSet<string> actionTools = [.. capabilities.SelectMany(capability => capability.ActionPolicies).SelectMany(policy => policy.ToolNames)];
        return [.. capabilities
            .SelectMany(capability => capability.Tools)
            .Where(tool => tool is not IAssistantPreparationTool &&
                           !actionTools.Contains(tool.Definition.Name) &&
                           !MutationOrPresentationTools.Contains(tool.Definition.Name))
            .Select(tool => tool.Definition.Name)];
    }
}
