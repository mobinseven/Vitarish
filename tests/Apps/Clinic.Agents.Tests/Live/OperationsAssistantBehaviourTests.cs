using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;
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

    private Task<AgentRunResult> RunAsync(string message, string? history = null)
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Compose(Now);
        return runtime.Runner.RunAsync(
            manifest.Definition,
            new AgentInput(message, history),
            manifest.Tools,
            LiveOperationsAssistant.ExecutionContext(Now),
            manifest.Limits with { Timeout = TimeSpan.FromMinutes(3) });
    }
}
