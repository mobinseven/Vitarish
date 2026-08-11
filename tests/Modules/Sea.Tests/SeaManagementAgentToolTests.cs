using FluentAssertions;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Infrastructure.Agents;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Tests;

public sealed class SeaManagementAgentToolTests
{
    [Fact]
    public async Task Model_triggered_mutation_only_prepares_an_action_without_calling_a_sea_write_service()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        SeaManagementAgentSubject subject = new(tenantId.ToString("D"), userId.ToString("D"));
        Mock<ISeaManagementAccessEvaluator> access = new(MockBehavior.Strict);
        access.Setup(value => value.IsAuthorizedAsync(subject, "create_project", "{}", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IProjectsService> projects = new(MockBehavior.Strict);
        Mock<IProceduresService> procedures = new(MockBehavior.Strict);
        Mock<IEventsService> events = new(MockBehavior.Strict);
        Mock<IAttachmentsService> attachments = new(MockBehavior.Strict);
        Mock<IReceiptsService> receipts = new(MockBehavior.Strict);
        Mock<IRolesService> roles = new(MockBehavior.Strict);
        Mock<IUserRolesService> userRoles = new(MockBehavior.Strict);
        Mock<IEventRolesService> eventRoles = new(MockBehavior.Strict);
        Mock<IDatabaseStatisticsService> statistics = new(MockBehavior.Strict);
        SeaManagementAgentTool tool = new(projects.Object, procedures.Object, events.Object, attachments.Object, receipts.Object, roles.Object, userRoles.Object, eventRoles.Object, statistics.Object, access.Object);

        AgentToolResult result = await tool.ExecuteAsync(new("call", "sea_manage", "{\"operation\":\"create_project\"}"), new("execution", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId.ToString("D"),
            ["authenticatedUserId"] = userId.ToString("D")
        }), CancellationToken.None);

        SeaManagementPreparedAction? prepared = tool.ParsePreparedAction(result);
        prepared.Should().NotBeNull();
        prepared!.Operation.Should().Be("create_project");
        prepared.PayloadJson.Should().Be("{}");
        prepared.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(29));
        projects.VerifyNoOtherCalls();
        procedures.VerifyNoOtherCalls();
        events.VerifyNoOtherCalls();
        attachments.VerifyNoOtherCalls();
        receipts.VerifyNoOtherCalls();
        roles.VerifyNoOtherCalls();
        userRoles.VerifyNoOtherCalls();
        eventRoles.VerifyNoOtherCalls();
        statistics.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Confirmed_reminder_uses_verified_contact_utc_and_server_owned_template_metadata()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        Guid procedureId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        string payload = $$"""{"sourceId":"{{eventId:D}}","recipientUserId":"{{userId:D}}","kind":"Email","remindAt":"2026-08-10T13:45:00+03:30","subject":"Subject","textBody":"Body"}""";
        SeaManagementAgentSubject subject = new(tenantId.ToString("D"), userId.ToString("D"));
        (SeaManagementAgentTool tool, Mock<IRemindersService> reminders, Mock<ISeaManagementAccessEvaluator> access) = CreateTool(tenantId, userId, eventId, procedureId, projectId);
        access.Setup(value => value.IsAuthorizedAsync(subject, "create_reminder", payload, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reminders.Setup(value => value.AddRangeAsync(eventId, It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(), "Subject", "Body", It.IsAny<DateTime>(), "SeaReminder", "event-reminder", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ReminderReadDto { Id = Guid.NewGuid(), SourceId = eventId, Recipient = "owner@example.test" }]);
        SeaManagementPreparedAction action = new(Guid.NewGuid(), "create_reminder", payload, DateTimeOffset.UtcNow.AddMinutes(30));

        await tool.ExecutePreparedAsync(new("call", "sea_manage", "{}"), action, subject, CancellationToken.None);

        reminders.Verify(value => value.AddRangeAsync(eventId, It.Is<IReadOnlyList<ReminderRecipientReadDto>>(recipients => recipients.Count == 1 && recipients[0].Kind == ReminderKind.Email && recipients[0].Recipient == "owner@example.test" && recipients[0].RecipientUserId == userId.ToString("D")), "Subject", "Body", new DateTime(2026, 8, 10, 10, 15, 0, DateTimeKind.Utc), "SeaReminder", "event-reminder", It.Is<string>(json => json.Contains(projectId.ToString("D"), StringComparison.Ordinal) && json.Contains(procedureId.ToString("D"), StringComparison.Ordinal) && json.Contains(eventId.ToString("D"), StringComparison.Ordinal)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reminder_rejects_unverified_contact_and_sent_records_are_immutable()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid eventId = Guid.NewGuid();
        (SeaManagementAgentTool tool, Mock<IRemindersService> reminders, Mock<ISeaManagementAccessEvaluator> access) = CreateTool(tenantId, userId, eventId, Guid.NewGuid(), Guid.NewGuid(), emailConfirmed: false);
        SeaManagementAgentSubject subject = new(tenantId.ToString("D"), userId.ToString("D"));
        string createPayload = $$"""{"sourceId":"{{eventId:D}}","recipientUserId":"{{userId:D}}","kind":"Email","remindAt":"2026-08-10T13:45:00+03:30","subject":"Subject","textBody":"Body"}""";
        access.Setup(value => value.IsAuthorizedAsync(subject, "create_reminder", createPayload, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SeaManagementPreparedAction create = new(Guid.NewGuid(), "create_reminder", createPayload, DateTimeOffset.UtcNow.AddMinutes(30));

        Func<Task> createCall = () => tool.ExecutePreparedAsync(new("call", "sea_manage", "{}"), create, subject, CancellationToken.None);
        await createCall.Should().ThrowAsync<ArgumentException>();

        Guid reminderId = Guid.NewGuid();
        ReminderReadDto sent = new() { Id = reminderId, SourceId = eventId, IsSent = true, SentAt = DateTime.UtcNow };
        reminders.Setup(value => value.GetAsync(reminderId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<ReminderReadDto>.Success(sent));
        string updatePayload = $$"""{"id":"{{reminderId:D}}","eventId":"{{eventId:D}}","recipientUserId":"{{userId:D}}","kind":"Sms","remindAt":"2026-08-10T13:45:00+03:30","subject":"Subject","textBody":"Body"}""";
        access.Setup(value => value.IsAuthorizedAsync(subject, "update_reminder", updatePayload, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        SeaManagementPreparedAction update = new(Guid.NewGuid(), "update_reminder", updatePayload, DateTimeOffset.UtcNow.AddMinutes(30));

        Func<Task> updateCall = () => tool.ExecutePreparedAsync(new("call", "sea_manage", "{}"), update, subject, CancellationToken.None);
        await updateCall.Should().ThrowAsync<InvalidOperationException>().WithMessage("Sent reminders are immutable.");
        reminders.Verify(value => value.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ReminderKind>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (SeaManagementAgentTool Tool, Mock<IRemindersService> Reminders, Mock<ISeaManagementAccessEvaluator> Access) CreateTool(Guid tenantId, Guid userId, Guid eventId, Guid procedureId, Guid projectId, bool emailConfirmed = true)
    {
        Mock<IProjectsService> projects = new();
        Mock<IProceduresService> procedures = new();
        Mock<IEventsService> events = new();
        Mock<IAttachmentsService> attachments = new();
        Mock<IReceiptsService> receipts = new();
        Mock<IRolesService> roles = new();
        Mock<IUserRolesService> userRoles = new();
        Mock<IEventRolesService> eventRoles = new();
        Mock<IDatabaseStatisticsService> statistics = new();
        Mock<ISeaManagementAccessEvaluator> access = new();
        Mock<IRemindersService> reminders = new();
        Mock<Vitarish.Users.Application.Abstractions.IUsersService> users = new();
        users.Setup(value => value.CheckUserRoleAsync(userId.ToString("D"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        users.Setup(value => value.GetUserAsync(userId.ToString("D"), It.IsAny<CancellationToken>())).ReturnsAsync(new User { Id = userId.ToString("D"), Email = "owner@example.test", EmailConfirmed = emailConfirmed, PhoneNumber = "+10000000000", PhoneNumberConfirmed = true });
        access.Setup(value => value.CanReadEventAsync(It.IsAny<SeaManagementAgentSubject>(), eventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        events.Setup(value => value.GetAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<EventReadDto>.Success(new EventReadDto { Id = eventId, ProcedureId = procedureId, ProjectId = projectId, Title = "Event" }));
        procedures.Setup(value => value.GetAsync(procedureId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<ProcedureReadDto>.Success(new ProcedureReadDto { Id = procedureId, Name = "Procedure" }));
        projects.Setup(value => value.GetAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(EntityResult<ProjectReadDto>.Success(new ProjectReadDto { Id = projectId, Name = "Project" }));
        return (new(projects.Object, procedures.Object, events.Object, attachments.Object, receipts.Object, roles.Object, userRoles.Object, eventRoles.Object, statistics.Object, access.Object, reminders.Object, users.Object), reminders, access);
    }
}
