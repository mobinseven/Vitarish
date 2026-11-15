using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Clinic.Application.Assistant.Staff;
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
        // It may count, list, or both; every read of that day carries the ISO date.
        runtime.Calls.Where(call => call.ToolName == "count_operations_appointments" || call.ToolName == "list_operations_appointments")
            .Should().NotBeEmpty()
            .And.OnlyContain(call => call.Arguments.GetProperty("dateFrom").GetString() == "2026-11-20");
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
        ScriptPatientSearch();

        // A plain request, as users type it: nothing asks for the result to be shown.
        (AgentRunResult result, string executionId) = await RunWithIdAsync("Find patient Sara.");

        PresentedSurface surface = await PresentedSurfaceAsync(result, executionId);
        string data = surface.DataModel.GetRawText();
        data.Should().Contain("tok-1").And.Contain("Sara Karimi", "the data comes from the search, by handle");
        surface.Components.GetRawText().Should().NotContain("Sara", "record values are never typed into the layout");
    }

    // Plain requests for each kind of record the operations assistant reads: the read must be shown
    // without the user asking for it to be shown. Production showed the model answering in text instead.
    [LiveAgentTheory]
    [InlineData("appointments", "What appointments do we have tomorrow?")]
    [InlineData("appointments", "نوبت‌های فردا رو بگو")]
    [InlineData("availability", "Which shifts still have free seats tomorrow?")]
    [InlineData("shifts", "What are our shifts?")]
    [InlineData("staff", "Who is on my team?")]
    [InlineData("setup", "How far along is my clinic setup?")]
    public async Task A_plain_request_shows_what_was_read(string scenario, string message)
    {
        runtime.Reset();
        ScriptOperationsReads();

        (AgentRunResult result, string executionId) = await RunWithIdAsync(message);

        PresentedSurface surface = await PresentedSurfaceAsync(result, executionId);
        surface.DataModel.GetRawText().Should().NotBe("{}", $"the {scenario} surface is filled from a read");
    }

    private async Task<PresentedSurface> PresentedSurfaceAsync(AgentRunResult result, string executionId)
    {
        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        string trace = "tool calls: " + string.Join(" | ", result.ToolExecutions.Select(execution =>
            $"{execution.ToolName}{(execution.IsError ? "(error)" : string.Empty)}:{execution.Output}")) + " | reply: " + result.Output;
        result.ToolExecutions.Should().Contain(execution => execution.ToolName == PresentSurfaceTool.Name && !execution.IsError,
            "the model shows what it read; " + trace);
        result.ToolExecutions.Where(execution => execution.ToolName == PresentSurfaceTool.Name)
            .Should().OnlyContain(execution => !execution.IsError, "the model's first surface is valid (ADR-0022: validity is measured); " + trace);
        IReadOnlyList<PresentedSurface> surfaces = await runtime.SurfacesAsync(executionId, result);
        return surfaces.Should().NotBeEmpty(trace).And.Subject.Last();
    }

    // Outputs in the shapes the real tools write (see ClinicAssistantPresentationsTests).
    private void ScriptPatientSearch() =>
        runtime.Script(ClinicPresentableToolNames.SearchPatients,
            "{\"Candidates\":[{\"CandidateToken\":\"tok-1\",\"DisplayName\":\"Sara Ahmadi\",\"DateOfBirth\":\"1990-04-02\",\"MaskedIdentification\":\"***1234\",\"MaskedPhone\":\"0912***1234\"},{\"CandidateToken\":\"tok-2\",\"DisplayName\":\"Sara Karimi\",\"DateOfBirth\":\"1985-01-20\",\"MaskedIdentification\":\"***9876\",\"MaskedPhone\":\"0935***5678\"}],\"HasMore\":false}");

    private void ScriptOperationsReads()
    {
        string tomorrow = DateOnly.FromDateTime(Now.UtcDateTime.AddDays(1)).ToString("yyyy-MM-dd");
        const string a = "11111111-1111-1111-1111-111111111111", b = "22222222-2222-2222-2222-222222222222", c = "33333333-3333-3333-3333-333333333333";
        runtime.Script(ClinicPresentableToolNames.ListOperationsAppointments,
            $"{{\"Status\":\"Succeeded\",\"Value\":[{{\"Id\":\"{a}\",\"Status\":\"Confirmed\",\"ShiftId\":\"{c}\",\"Date\":\"{tomorrow}\",\"EstimatedSessionLength\":\"00:30:00\",\"ConfirmedAt\":\"{tomorrow}T07:00:00\",\"CancelledAt\":null,\"IsHold\":false,\"ExpiresAt\":null}},{{\"Id\":\"{b}\",\"Status\":\"Pending\",\"ShiftId\":\"{c}\",\"Date\":\"{tomorrow}\",\"EstimatedSessionLength\":\"00:30:00\",\"ConfirmedAt\":null,\"CancelledAt\":null,\"IsHold\":false,\"ExpiresAt\":null}}],\"OperationId\":null,\"Message\":null}}");
        runtime.Script(ClinicPresentableToolNames.CountOperationsAppointments, "{\"Status\":0,\"Value\":2,\"OperationId\":null,\"Message\":null}");
        runtime.Script(ClinicPresentableToolNames.GetOperationsAvailability,
            $"{{\"Status\":0,\"Value\":[{{\"Shift\":{{\"id\":\"{c}\",\"Status\":1}},\"Occurrence\":{{\"ShiftId\":\"{c}\",\"Date\":\"{tomorrow}\",\"StartTime\":\"08:00:00\",\"EndTime\":\"12:00:00\",\"TotalSeats\":4,\"ReservationCount\":1,\"AvailableSeats\":3,\"IsClosed\":false}}}}]}}");
        runtime.Script(ClinicPresentableToolNames.ListOperationsShifts,
            $"{{\"Status\":0,\"Value\":[{{\"Status\":1,\"Visibility\":0,\"DayOfWeek\":1,\"StartTime\":\"08:00:00\",\"EndTime\":\"12:00:00\",\"TimeZone\":\"UTC\",\"TotalSeats\":4,\"ServiceProviderId\":\"{b}\",\"ServiceId\":null,\"id\":\"{c}\"}}]}}");
        runtime.Script(AssistantStaffManagement.ListToolName,
            "{\"members\":[{\"userId\":\"user-9\",\"name\":\"Mina Rad\",\"role\":\"staff\",\"contact\":\"09******11\"},{\"userId\":\"user-1\",\"name\":\"Ali Nouri\",\"role\":\"owner\",\"contact\":\"09******22\"}]}");
        runtime.Script(ClinicPresentableToolNames.GetClinicSetupProgress,
            "{\"goals\":[{\"goal\":\"ClinicProfile\",\"done\":true},{\"goal\":\"StaffUsers\",\"done\":false},{\"goal\":\"ShiftsAndBookingRules\",\"done\":false}],\"nextGoal\":\"StaffUsers\",\"workingHours\":[]}");
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
