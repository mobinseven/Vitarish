using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests.Live;

/// <summary>
/// The operations assistant's behaviour against the real model and runtime: which tools it chooses,
/// with which arguments, and whether a conversation continues after its configuration changes.
/// </summary>
[Collection(LiveAgentCollection.Name)]
public sealed class OperationsAssistantBehaviourTests(LiveAgentRuntime runtime)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // Tool choice is only meaningful when the competing tools are present, as they are in production.
    [Fact]
    public void The_spec_offers_the_model_every_operations_capability()
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Compose(Now);

        manifest.ToolCapabilities.Values.Distinct().Should().Contain(AssistantCapabilityKeys.OperationsProfile);
    }

    [LiveAgentFact]
    public async Task Finding_a_patient_by_name_searches_patients_with_that_name()
    {
        runtime.Reset();

        AgentRunResult result = await RunAsync("Find the patient Sara Ahmadi.");

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        RecordedToolCall search = runtime.Calls.Should().Contain(call => call.ToolName == "search_patients").Which;
        search.Arguments.GetProperty("search").GetString().Should().ContainEquivalentOf("Sara");
    }

    [LiveAgentFact]
    public async Task Asking_about_a_day_reads_appointments_with_an_iso_date()
    {
        runtime.Reset();

        AgentRunResult result = await RunAsync("How many appointments do we have on 2026-11-20?");

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        RecordedToolCall read = runtime.Calls.Should()
            .Contain(call => call.ToolName == "count_operations_appointments" || call.ToolName == "list_operations_appointments").Which;
        read.Arguments.GetProperty("dateFrom").GetString().Should().Be("2026-11-20");
    }

    [LiveAgentFact]
    public async Task A_conversation_continues_on_its_carried_over_history_after_the_configuration_changes()
    {
        runtime.Reset();
        runtime.Script("search_patients",
            "{\"candidates\":[{\"candidateToken\":\"cand_1\",\"displayName\":\"Sara Ahmadi\",\"birthDate\":\"1990-04-02\"}]}");
        AgentRunResult first = await RunAsync("Find the patient Sara Ahmadi.");
        first.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(first));
        first.History.Should().Contain("function_call", "the first turn used a tool, so its history holds tool items");

        string? carried = AgentConversationHistory.CarryOver(first.History);
        AgentRunResult next = await RunAsync("What was the name of the patient I just asked about?", carried);

        next.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(next));
        next.Output.Should().ContainEquivalentOf("Sara");
    }

    [LiveAgentFact]
    public async Task Patients_it_finds_are_presented_from_the_search_result_not_typed_by_the_model()
    {
        runtime.Reset();
        // The real search output: the Operations serializer writes PascalCase.
        runtime.Script(ClinicPresentableToolNames.SearchPatients,
            "{\"Candidates\":[{\"CandidateToken\":\"tok-1\",\"DisplayName\":\"Sara Ahmadi\",\"DateOfBirth\":\"1990-04-02\",\"MaskedIdentification\":\"***1234\",\"MaskedPhone\":\"0912***1234\"},{\"CandidateToken\":\"tok-2\",\"DisplayName\":\"Sara Karimi\",\"DateOfBirth\":\"1985-01-20\",\"MaskedIdentification\":\"***9876\",\"MaskedPhone\":\"0935***5678\"}],\"HasMore\":false}");

        (AgentRunResult result, string executionId) = await RunWithIdAsync("Show me the patients named Sara.");

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        result.ToolExecutions.Should().Contain(execution => execution.ToolName == PresentSurfaceTool.Name && !execution.IsError,
            "the model presents what it found; tool outputs: " + string.Join(" | ", result.ToolExecutions.Select(execution => $"{execution.ToolName}:{execution.Output}")));
        IReadOnlyList<PresentedSurface> surfaces = await runtime.SurfacesAsync(executionId, result);
        PresentedSurface surface = surfaces.Should().ContainSingle().Which;
        string data = surface.DataModel.GetRawText();
        data.Should().Contain("tok-1").And.Contain("Sara Karimi", "the data comes from the search, by handle");
        surface.Components.GetRawText().Should().NotContain("Sara", "record values are never typed into the layout");
    }

    private async Task<AgentRunResult> RunAsync(string message, string? history = null) =>
        (await RunWithIdAsync(message, history)).Result;

    private Task<(AgentRunResult Result, string ExecutionId)> RunWithIdAsync(string message, string? history = null)
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Compose(Now);
        AgentExecutionContext context = LiveOperationsAssistant.ExecutionContext(Now);
        return Run();

        async Task<(AgentRunResult, string)> Run() => (await runtime.Runner.RunAsync(
            manifest.Definition,
            new AgentInput(message, history),
            manifest.Tools,
            context,
            manifest.Limits with { Timeout = TimeSpan.FromMinutes(3) }), context.ExecutionId);
    }
}
