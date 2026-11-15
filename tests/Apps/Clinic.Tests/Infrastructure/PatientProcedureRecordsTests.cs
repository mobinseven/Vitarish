using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class PatientProcedureRecordsTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid OtherPatientId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IProceduresService> procedures = new();
    private readonly Mock<IEventsService> events = new();
    private readonly Mock<IAttachmentsService> attachments = new();
    private readonly Mock<ISeaManagementAccessEvaluator> access = new();
    private readonly Mock<IAssistantPatientSearch> search = new();
    private readonly EventReadDto done = Step(1, committed: true);
    private readonly EventReadDto hidden = Step(2, committed: false);
    private readonly EventReadDto open = Step(3, committed: false);

    public PatientProcedureRecordsTests()
    {
        ProcedureReadDto procedure = new() { Id = ProcedureId, ProjectId = PatientId, Name = "Implant", CreatedAt = DateTime.UtcNow };
        procedures.Setup(service => service.GetListAsync(It.Is<ProcedureDescriptorDto>(d => d.ProjectId == PatientId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ProcedureReadDto>>.Success([procedure]));
        procedures.Setup(service => service.GetAsync(ProcedureId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<ProcedureReadDto>.Success(procedure));
        events.Setup(service => service.GetByProcedureAsync(ProcedureId, It.IsAny<CancellationToken>())).ReturnsAsync([done, hidden, open]);
        foreach (EventReadDto step in new[] { done, hidden, open })
            events.Setup(service => service.GetAsync(step.Id, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<EventReadDto>.Success(step));
        access.Setup(evaluator => evaluator.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SeaManagementAgentSubject subject, Guid id, CancellationToken _) => subject.UserId == UserId.ToString("D") && id != hidden.Id);
    }

    private PatientProcedureRecords Records() => new(procedures.Object, events.Object, attachments.Object, access.Object, search.Object);

    private static AgentExecutionContext Context() => new("run-1", new Dictionary<string, string>
    {
        ["practiceId"] = "practice-1",
        ["authenticatedUserId"] = UserId.ToString("D"),
        ["patientId"] = PatientId.ToString("D")
    });

    [Fact]
    public async Task Progress_gives_the_last_committed_step_and_the_first_open_step_withholding_a_restricted_one()
    {
        ProcedureListResponse result = await Records().ListProceduresAsync(PatientId, Context(), 10, CancellationToken.None);

        ProcedureSummary procedure = result.Procedures.Single();
        procedure.StepCount.Should().Be(3);
        procedure.CommittedStepCount.Should().Be(1);
        procedure.CurrentStep.Should().BeEquivalentTo(new StepReference(done.Id, done.Title, 1, done.CommittedAt, false));
        procedure.NextStep.Should().BeEquivalentTo(new StepReference(null, null, 2, null, true));
    }

    [Fact]
    public async Task Steps_are_listed_in_order_and_a_restricted_step_shows_only_its_position()
    {
        StepListResponse result = await Records().ListStepsAsync(PatientId, ProcedureId, Context(), 10, CancellationToken.None);

        result.Steps!.Select(step => step.Position).Should().Equal(1, 2, 3);
        result.Steps![1].Should().BeEquivalentTo(new StepSummary(null, null, 2, false, null, null, null, true));
        result.Steps![2].Title.Should().Be(open.Title);
    }

    [Fact]
    public async Task Another_patients_procedure_or_step_is_not_found_and_a_restricted_step_is_never_read()
    {
        (await Records().ListStepsAsync(OtherPatientId, ProcedureId, Context(), 10, CancellationToken.None)).Found.Should().BeFalse();
        (await Records().GetStepAsync(OtherPatientId, done.Id, Context(), CancellationToken.None)).Found.Should().BeFalse();

        StepDetailResponse restricted = await Records().GetStepAsync(PatientId, hidden.Id, Context(), CancellationToken.None);

        restricted.Should().BeEquivalentTo(new StepDetailResponse(false, true));
        attachments.Verify(service => service.GetByEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_step_carries_its_latest_notes_as_text_its_files_as_metadata_and_its_reminder_count()
    {
        string longNote = new('x', PatientProcedureRecords.MaxNoteLength + 5);
        attachments.Setup(service => service.GetByEventAsync(done.Id, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new AttachmentReadDto { Id = Guid.NewGuid(), EventId = done.Id, Kind = AttachmentKind.Text, Content = longNote, ContentType = "text/plain", CreatedAt = DateTime.UtcNow },
            new AttachmentReadDto { Id = Guid.NewGuid(), EventId = done.Id, Kind = AttachmentKind.File, Content = "BINARY", ContentType = "image/png", FileName = "scan.png", Size = 10, CreatedAt = DateTime.UtcNow }
        ]);
        events.Setup(service => service.GetRemindersCountAsync(done.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        StepDetail step = (await Records().GetStepAsync(PatientId, done.Id, Context(), CancellationToken.None)).Step!;

        step.Position.Should().Be(1);
        step.ProcedureName.Should().Be("Implant");
        step.Notes.Single().Text.Should().HaveLength(PatientProcedureRecords.MaxNoteLength);
        step.Notes.Single().Truncated.Should().BeTrue();
        step.Files.Single().FileName.Should().Be("scan.png");
        JsonSerializer.Serialize(step).Should().NotContain("BINARY");
        step.ReminderCount.Should().Be(2);
    }

    [Fact]
    public async Task The_practice_workspace_needs_a_candidate_token_from_this_run()
    {
        search.Setup(service => service.ResolveAsync("practice-1", UserId.ToString("D"), "run-1", "good", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssistantCandidateReference(PatientId, "practice-1", UserId.ToString("D"), "run-1", "Sara", "***", "***"));
        using JsonDocument good = JsonDocument.Parse("""{"candidateToken":"good"}""");
        using JsonDocument expired = JsonDocument.Parse("""{"candidateToken":"old"}""");
        using JsonDocument none = JsonDocument.Parse("{}");

        (await Records().ResolvePatientAsync(ProcedureToolWorkspace.Operations, good.RootElement, Context(), CancellationToken.None)).Should().Be(PatientId);
        await FluentActions.Awaiting(() => Records().ResolvePatientAsync(ProcedureToolWorkspace.Operations, expired.RootElement, Context(), CancellationToken.None))
            .Should().ThrowAsync<JsonException>().WithMessage("*search for the patient again*");
        await FluentActions.Awaiting(() => Records().ResolvePatientAsync(ProcedureToolWorkspace.Operations, none.RootElement, Context(), CancellationToken.None))
            .Should().ThrowAsync<JsonException>();
        (await Records().ResolvePatientAsync(ProcedureToolWorkspace.Patient, good.RootElement, Context(), CancellationToken.None)).Should().Be(PatientId);
    }

    private static EventReadDto Step(int position, bool committed) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = PatientId,
        ProcedureId = ProcedureId,
        Title = $"Step {position}",
        Description = "details",
        CommittedBy = committed ? Guid.NewGuid() : default,
        CommittedAt = committed ? DateTime.UtcNow : null,
        CreatedAt = DateTime.UtcNow
    };
}
