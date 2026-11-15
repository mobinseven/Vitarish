using FluentAssertions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Presentation;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;
using Xunit;

namespace Vitarish.Clinic.Agents.Tests.Live;

/// <summary>
/// Whether both Clinic assistants understand what procedures and steps are, whatever words the user picks
/// (ADR-0023 decision 7). Each phrasing must reach the procedure reads, not a guess or a question back.
/// </summary>
[Collection(LiveAgentCollection.Name)]
public sealed class ProcedureUnderstandingBehaviourTests(LiveAgentRuntime runtime)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private const string ProcedureId = "5b0c7f0e-2f9a-4b1e-9f43-0d8f6c1a2b3c";
    private const string StepId = "9e1d2c3b-4a5f-4e6d-8c7b-1a2b3c4d5e6f";
    private const string CandidateToken = "candidate-live-1";

    private static readonly string Progress =
        """{"procedures":[{"id":"@PROCEDURE","name":"Implant treatment","stats":{"state":"Half","events":4,"committedEvents":2},"sourceUpdatedAt":"2026-11-10T09:00:00Z","stepCount":4,"committedStepCount":2,"currentStep":{"id":"@STEP","title":"Fixture placement","position":2,"committedAt":"2026-11-10T09:00:00Z","restricted":false},"nextStep":{"id":"1c2d3e4f-5a6b-4c7d-8e9f-0a1b2c3d4e5f","title":"Healing check","position":3,"restricted":false}}],"hasMore":false}""".Replace("@PROCEDURE", ProcedureId).Replace("@STEP", StepId);

    private static readonly string Step =
        """{"found":true,"restricted":false,"step":{"id":"@STEP","title":"Fixture placement","description":"","position":2,"isCommitted":true,"committedAt":"2026-11-10T09:00:00Z","sourceUpdatedAt":"2026-11-10T09:00:00Z","procedureId":"@PROCEDURE","procedureName":"Implant treatment","notes":[{"id":"2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e","text":"Torque 35 Ncm, no complications.","truncated":false,"createdAt":"2026-11-10T09:05:00Z"}],"moreNotes":false,"files":[],"moreFiles":false,"reminderCount":1}}""".Replace("@PROCEDURE", ProcedureId).Replace("@STEP", StepId);

    [LiveAgentTheory]
    [InlineData("Report the patient's procedure latest state.")]
    [InlineData("Where are we with this patient's treatment?")]
    [InlineData("How far along is the implant?")]
    [InlineData("وضعیت آخر روند درمان بیمار را گزارش کن.")]
    [InlineData("درمان این بیمار به کجا رسیده؟")]
    public async Task On_the_patient_page_any_wording_of_where_a_treatment_stands_reads_procedure_progress(string message)
    {
        runtime.Reset();
        runtime.Script(ClinicPresentableToolNames.ListPatientProcedureProgress, Progress);

        AgentRunResult result = await RunPatientAsync(message);

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        runtime.Calls.Should().Contain(call => call.ToolName == ClinicPresentableToolNames.ListPatientProcedureProgress);
    }

    [LiveAgentTheory]
    [InlineData("What was noted in the last session?")]
    [InlineData("Any notes on the last completed step?")]
    [InlineData("در جلسه آخر چه یادداشتی ثبت شده؟")]
    public async Task On_the_patient_page_notes_of_the_last_step_are_read_from_that_step(string message)
    {
        runtime.Reset();
        runtime.Script(ClinicPresentableToolNames.ListPatientProcedureProgress, Progress);
        runtime.Script(ClinicPresentableToolNames.GetPatientProcedureStep, Step);

        AgentRunResult result = await RunPatientAsync(message);

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        runtime.Calls.Should().Contain(call => call.ToolName == ClinicPresentableToolNames.GetPatientProcedureStep &&
            call.Arguments.GetProperty("stepId").GetString() == StepId);
    }

    [LiveAgentTheory]
    [InlineData("Where is Sara Ahmadi's treatment at?")]
    [InlineData("روند درمان سارا احمدی در چه مرحله‌ای است؟")]
    public async Task In_the_practice_workspace_a_patients_treatment_is_read_after_finding_the_patient(string message)
    {
        runtime.Reset();
        runtime.Script(ClinicPresentableToolNames.SearchPatients,
            $$"""{"candidates":[{"candidateToken":"{{CandidateToken}}","displayName":"Sara Ahmadi","dateOfBirth":"1990-02-03","maskedIdentification":"***1234","maskedPhone":"***5678"}],"hasMore":false}""");
        runtime.Script(ClinicPresentableToolNames.ListOperationsPatientProcedures, Progress);

        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Compose(Now);
        AgentRunResult result = await runtime.Runner.RunAsync(manifest.Definition, new AgentInput(message), manifest.Tools,
            LiveOperationsAssistant.ExecutionContext(Now), manifest.Limits with { Timeout = TimeSpan.FromMinutes(3) });

        result.IsSuccess.Should().BeTrue(await runtime.FailureReasonAsync(result));
        runtime.Calls.Should().Contain(call => call.ToolName == ClinicPresentableToolNames.ListOperationsPatientProcedures &&
            call.Arguments.GetProperty("candidateToken").GetString() == CandidateToken);
    }

    private async Task<AgentRunResult> RunPatientAsync(string message)
    {
        ComposedAgentManifest<AssistantCompositionContext> manifest = LiveOperationsAssistant.Patient.Compose(Now);
        return await runtime.Runner.RunAsync(manifest.Definition, new AgentInput(message), manifest.Tools,
            LiveOperationsAssistant.Patient.ExecutionContext(Now), manifest.Limits with { Timeout = TimeSpan.FromMinutes(3) });
    }
}
