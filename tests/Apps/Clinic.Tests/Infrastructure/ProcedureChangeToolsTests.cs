using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Infrastructure.Assistant.Actions;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Agents;
using Names = Vitarish.Clinic.Application.Assistant.Presentation.ClinicPresentableToolNames;

namespace Vitarish.Clinic.Tests.Infrastructure;

/// <summary>
/// The typed procedure-change tools run through the real SEA management tool: SEA authorizes at preparation, the
/// stored confirmation is what SEA executes, and the payload shapes are the ones SEA reads.
/// </summary>
public sealed class ProcedureChangeToolsTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid ProcedureId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IProjectsService> projects = new();
    private readonly Mock<IProceduresService> procedures = new();
    private readonly Mock<IEventsService> events = new();
    private readonly Mock<IAttachmentsService> attachments = new();
    private readonly Mock<ISeaManagementAccessEvaluator> access = new();
    private readonly EventReadDto step = new()
    {
        Id = Guid.NewGuid(), ProjectId = PatientId, ProcedureId = ProcedureId, Title = "Fixture placement", Description = "", CreatedAt = DateTime.UtcNow
    };
    private readonly SeaManagementAgentTool workflow;

    public ProcedureChangeToolsTests()
    {
        ProcedureReadDto procedure = new() { Id = ProcedureId, ProjectId = PatientId, Name = "Implant", CreatedAt = DateTime.UtcNow };
        procedures.Setup(service => service.GetAsync(ProcedureId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<ProcedureReadDto>.Success(procedure));
        events.Setup(service => service.GetAsync(step.Id, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<EventReadDto>.Success(step));
        events.Setup(service => service.GetByProcedureAsync(ProcedureId, It.IsAny<CancellationToken>())).ReturnsAsync([step]);
        attachments.Setup(service => service.GetByEventAsync(step.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        access.Setup(evaluator => evaluator.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), step.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        access.Setup(evaluator => evaluator.IsAuthorizedAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        workflow = new(projects.Object, procedures.Object, events.Object, attachments.Object, Mock.Of<IReceiptsService>(), Mock.Of<IRolesService>(),
            Mock.Of<IUserRolesService>(), Mock.Of<IEventRolesService>(), Mock.Of<IDatabaseStatisticsService>(), access.Object);
    }

    private IAgentTool Tool(string name, ProcedureToolWorkspace workspace = ProcedureToolWorkspace.Patient)
    {
        PatientProcedureRecords records = new(procedures.Object, events.Object, attachments.Object, access.Object, Mock.Of<IAssistantPatientSearch>());
        IAssistantCapability capability = workspace == ProcedureToolWorkspace.Patient
            ? new ProcedureChangeCapability(records, workflow)
            : new OperationsProcedureChangeCapability(records, workflow);
        return capability.Tools.Single(tool => tool.Definition.Name == name);
    }

    private static AgentExecutionContext Context() => new("run-1", new Dictionary<string, string>
    {
        ["practiceId"] = "practice-1",
        ["authenticatedUserId"] = UserId.ToString("D"),
        ["patientId"] = PatientId.ToString("D")
    });

    private async Task<(AgentToolResult Result, AssistantPreparedAction? Prepared)> PrepareAsync(string name, string arguments)
    {
        IAgentTool tool = Tool(name);
        AgentToolResult result = await tool.ExecuteAsync(new("call-1", name, arguments), Context());
        return (result, ((IAssistantPreparationTool)tool).GetPreparedAction(result));
    }

    private async Task<AssistantActionCommitResult> ConfirmAsync(AssistantPreparedAction prepared) =>
        await new ProcedureChangeActionCommitter(workflow).CommitAsync(
            new AssistantAction(prepared.OwningId, "run-1", "conversation-1", PatientId, "practice-1", UserId.ToString("D"), prepared.Capability,
                prepared.ActionType, prepared.OwningId, prepared.ReviewDataJson, AssistantActionStatus.Confirming, DateTimeOffset.UtcNow, prepared.Expiration!.Value, ToolCallId: "call-1"),
            new AssistantContext("practice-1", PatientId, UserId.ToString("D"), new HashSet<string>(), "TenantUser", "en-US", "UTC", "conversation-1"),
            CancellationToken.None);

    [Fact]
    public async Task A_note_is_prepared_for_review_with_the_steps_name_and_written_as_the_step_screen_writes_it_once_confirmed()
    {
        attachments.Setup(service => service.AddAsync(step.Id, "Torque 35 Ncm.", "text/plain", null, 14, null, AttachmentKind.Text, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<AttachmentReadDto>.Success(new AttachmentReadDto { Id = Guid.NewGuid(), EventId = step.Id, Content = "Torque 35 Ncm.", ContentType = "text/plain" }));

        (AgentToolResult result, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PrepareStepNote, $$"""{"stepId":"{{step.Id}}","text":"Torque 35 Ncm."}""");

        prepared.Should().NotBeNull(result.Output);
        prepared!.ActionType.Should().Be(ProcedureActionTypes.StepChange);
        using JsonDocument review = JsonDocument.Parse(prepared.ReviewDataJson);
        review.RootElement.GetProperty("change").GetProperty("kind").GetString().Should().Be(ProcedureChangeKinds.AddNote);
        review.RootElement.GetProperty("change").GetProperty("stepTitle").GetString().Should().Be("Fixture placement");
        review.RootElement.GetProperty("change").GetProperty("procedureName").GetString().Should().Be("Implant");
        attachments.Verify(service => service.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<AttachmentKind>(), It.IsAny<CancellationToken>()), Times.Never);

        (await ConfirmAsync(prepared)).Succeeded.Should().BeTrue();
        attachments.Verify(service => service.AddAsync(step.Id, "Torque 35 Ncm.", "text/plain", null, 14, null, AttachmentKind.Text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Committing_a_step_commits_it_as_the_confirming_user()
    {
        (_, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PrepareStepStatus, $$"""{"stepId":"{{step.Id}}","status":"committed"}""");

        (await ConfirmAsync(prepared!)).Succeeded.Should().BeTrue();
        events.Verify(service => service.CommitAsync(step.Id, UserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_new_procedure_is_created_for_this_patient_in_the_shape_the_workflow_reads()
    {
        procedures.Setup(service => service.AddAsync(It.IsAny<ProcedureWriteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ProcedureReadDto>.Success(new ProcedureReadDto { Id = Guid.NewGuid(), ProjectId = PatientId, Name = "Orthodontics" }));

        (_, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PreparePatientProcedure, """{"name":"Orthodontics","description":"Upper arch"}""");

        prepared!.ActionType.Should().Be(ProcedureActionTypes.Create);
        (await ConfirmAsync(prepared)).Succeeded.Should().BeTrue();
        procedures.Verify(service => service.AddAsync(It.Is<ProcedureWriteDto>(dto => dto.ProjectId == PatientId && dto.Name == "Orthodontics" && dto.Description == "Upper arch"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task When_the_workflow_refuses_the_model_is_told_the_change_is_not_permitted_and_nothing_is_prepared()
    {
        access.Setup(evaluator => evaluator.IsAuthorizedAsync(It.IsAny<SeaManagementAgentSubject>(), SeaManagementOperations.CommitEvent, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        (AgentToolResult result, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PrepareStepStatus, $$"""{"stepId":"{{step.Id}}","status":"committed"}""");

        prepared.Should().BeNull();
        result.Output.Should().Contain(ProcedureChangeResult.NotPermitted);
    }

    [Fact]
    public async Task Another_patients_step_is_not_found_and_never_reaches_the_workflow()
    {
        step.ProjectId = Guid.NewGuid();

        (AgentToolResult result, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PrepareStepNote, $$"""{"stepId":"{{step.Id}}","text":"note"}""");

        prepared.Should().BeNull();
        result.Output.Should().Contain(ProcedureChangeResult.NotFound);
        access.Verify(evaluator => evaluator.IsAuthorizedAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_confirmation_refused_by_the_workflow_fails_as_not_permitted()
    {
        (_, AssistantPreparedAction? prepared) = await PrepareAsync(Names.PrepareStepStatus, $$"""{"stepId":"{{step.Id}}","status":"open"}""");
        access.Setup(evaluator => evaluator.IsAuthorizedAsync(It.IsAny<SeaManagementAgentSubject>(), SeaManagementOperations.UncommitEvent, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        AssistantActionCommitResult result = await ConfirmAsync(prepared!);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Code.Should().Be("not_permitted");
        events.Verify(service => service.UncommitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task The_care_team_tool_refuses_operations_on_patients_records()
    {
        SeaManagementClinicToolAdapter adapter = new(workflow);

        await FluentActions.Awaiting(() => adapter.ExecuteAsync(new("call-1", Names.ManageCareTeamAccess, """{"operation":"create_project","payload":{}}"""), Context()))
            .Should().ThrowAsync<JsonException>();
        access.Verify(evaluator => evaluator.IsAuthorizedAsync(It.IsAny<SeaManagementAgentSubject>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Only_practice_owners_are_offered_adding_a_procedure_and_every_change_needs_confirmation()
    {
        ProcedureChangeCapability capability = new(new(procedures.Object, events.Object, attachments.Object, access.Object, Mock.Of<IAssistantPatientSearch>()), workflow);

        capability.ActionPolicies.Should().OnlyContain(policy => policy.RequiresConfirmation);
        capability.ActionPolicies.Single(policy => policy.ActionType == ProcedureActionTypes.Create).RequiredRoles
            .Should().BeEquivalentTo([AssistantOperationsScope.TenantOwnerRole]);
    }
}
