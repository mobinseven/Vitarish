using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.BackgroundJobs.Sea;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure.Services;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class ReminderProcessorTests : IDisposable
{
    private readonly Mock<ILogger<ReminderProcessor>> mockLogger;
    private readonly Mock<IRemindersService> mockRemindersService;
    private readonly Mock<IEventsService> mockEventsService;
    private readonly Mock<IMessagesService> mockMessagesService;
    private readonly Mock<IJobCancellationToken> mockCancellationToken;
    private readonly Mock<IPartiesService> mockPartiesService;
    private readonly FakeTimeProvider fakeTimeProvider;
    private readonly ServiceProvider serviceProvider;
    private readonly RequestContextRecorder contextRecorder;
    private readonly ReminderProcessor sut;

    public ReminderProcessorTests()
    {
        mockLogger = new Mock<ILogger<ReminderProcessor>>();
        mockRemindersService = new Mock<IRemindersService>();
        mockEventsService = new Mock<IEventsService>();
        mockMessagesService = new Mock<IMessagesService>();
        mockCancellationToken = new Mock<IJobCancellationToken>();
        mockPartiesService = new Mock<IPartiesService>();
        fakeTimeProvider = new FakeTimeProvider();

        // Setup default cancellation token behavior
        mockCancellationToken.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Tenant
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    ClientId = "client-1",
                    Name = "Tenant 1"
                }
            });

        ServiceCollection services = new();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());
        services.AddSingleton<RequestContextRecorder>();
        services.AddScoped(sp =>
        {
            RequestContextRecorder recorder = sp.GetRequiredService<RequestContextRecorder>();
            IRequestContext context = sp.GetRequiredService<IRequestContext>();
            recorder.Record(context);
            return mockRemindersService.Object;
        });
        services.AddScoped(_ => mockEventsService.Object);
        services.AddScoped(_ => mockMessagesService.Object);
        services.AddScoped(_ => mockPartiesService.Object);
        services.AddScoped<TenantBackgroundExecutor>();

        serviceProvider = services.BuildServiceProvider();
        contextRecorder = serviceProvider.GetRequiredService<RequestContextRecorder>();

        sut = new ReminderProcessor(
            mockLogger.Object,
            serviceProvider.GetRequiredService<TenantBackgroundExecutor>(),
            fakeTimeProvider);
    }

    public void Dispose() => serviceProvider.Dispose();

    private class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
        private TimeZoneInfo _localTimeZone = TimeZoneInfo.Local;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
        public void SetLocalTimeZone(TimeZoneInfo timeZone) => _localTimeZone = timeZone;

        public override DateTimeOffset GetUtcNow() => _utcNow;
        public override TimeZoneInfo LocalTimeZone => _localTimeZone;
    }

    private sealed class RequestContextRecorder
    {
        public List<(string? TenantId, string OwnerId, string ClientId)> Snapshots { get; } = [];

        public void Record(IRequestContext context) =>
            Snapshots.Add((context.TenantId, context.OwnerId, context.ClientId));
    }

    [Fact]
    public async Task ProcessDueReminders_WithNoReminders_DoesNotProcessAny()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);
        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReminderDto>());

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockEventsService.Verify(
            x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessDueReminders_WithEmailReminder_SendsEmail()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto(
            kind: ReminderKind.Email,
            recipient: "test@example.com");

        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockMessagesService.Verify(
            x => x.SendEmail(
                It.Is<Message>(m =>
                    m.Recipient == reminder.Recipient &&
                    m.Subject == reminder.Subject &&
                    m.Body == reminder.TextBody),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockRemindersService.Verify(
            x => x.MarkSentAsync(reminder.Id, now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_WithSmsReminder_SendsSms()
    {
        // Arrange
        DateTimeOffset now = new(2024, 6, 15, 14, 0, 0, TimeSpan.Zero); // 2 PM UTC
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto(
            kind: ReminderKind.Sms,
            recipient: "+1234567890");

        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        mockMessagesService
            .Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult("msg-123", null, 0, null, null, null, null));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockMessagesService.Verify(
            x => x.SendSms(
                It.Is<Message>(m =>
                    m.Recipient == reminder.Recipient &&
                    m.Body == reminder.TextBody),
                It.IsAny<CancellationToken>()),
            Times.Once);

        mockRemindersService.Verify(
            x => x.MarkSentAsync(reminder.Id, now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_WithSmsReminderDuringQuietHours_DefersReminder()
    {
        // Arrange - 3 AM local time
        DateTimeOffset now = new(2024, 6, 15, 3, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(now);
        // In local time this is 3 AM which is during quiet hours

        ReminderDto reminder = CreateReminderDto(
            kind: ReminderKind.Sms,
            recipient: "+1234567890");

        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockMessagesService.Verify(
            x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockRemindersService.Verify(
            x => x.MarkSentAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessDueReminders_WhenEventNotFound_MarksReminderAsSent()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Failure([new EntityError(EntityErrorCode.NotFound, "Event not found")]));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockMessagesService.Verify(
            x => x.SendEmail(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never);

        mockRemindersService.Verify(
            x => x.MarkSentAsync(reminder.Id, now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_WhenSmsSendReturnsNoMessageId_MarksReminderAsSent()
    {
        // Arrange
        DateTimeOffset now = new(2024, 6, 15, 14, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto(
            kind: ReminderKind.Sms,
            recipient: "+1234567890");

        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        mockMessagesService
            .Setup(x => x.SendSms(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsSendResult(string.Empty, null, 0, null, null, null, null));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockRemindersService.Verify(
            x => x.MarkSentAsync(reminder.Id, now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_WhenExceptionOccurs_LogsErrorAndContinues()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder1 = CreateReminderDto();
        ReminderDto reminder2 = CreateReminderDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder1, reminder2 });

        mockEventsService
            .Setup(x => x.GetAsync(reminder1.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        mockEventsService
            .Setup(x => x.GetAsync(reminder2.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(CreateEventDto()));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_WithHtmlContentInTextBody_EncodesHtml()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto(
            kind: ReminderKind.Email,
            recipient: "test@example.com",
            textBody: "<script>alert('xss')</script>");

        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockMessagesService.Verify(
            x => x.SendEmail(
                It.Is<Message>(m =>
                    m.HtmlBody != null &&
                    m.HtmlBody.Contains("&lt;script&gt;") &&
                    m.HtmlBody.Contains("&lt;/script&gt;")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessDueReminders_IteratesAllTenants()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        fakeTimeProvider.SetUtcNow(now);

        ReminderDto reminder = CreateReminderDto();
        EventDto evt = CreateEventDto();

        mockRemindersService
            .Setup(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reminder });

        mockEventsService
            .Setup(x => x.GetAsync(reminder.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<EventDto>.Success(evt));

        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Tenant
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    ClientId = "client-1",
                    Name = "Tenant 1"
                },
                new Tenant
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000020"),
                    ClientId = "client-2",
                    Name = "Tenant 2"
                }
            });

        // Act
        await sut.ProcessDueReminders(mockCancellationToken.Object);

        // Assert
        mockRemindersService.Verify(x => x.GetDueAsync(now, It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockRemindersService.Verify(x => x.MarkSentAsync(reminder.Id, now, It.IsAny<CancellationToken>()), Times.Exactly(2));
        contextRecorder.Snapshots.Should().BeEquivalentTo(new[]
        {
            ("00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000010", "client-1"),
            ("00000000-0000-0000-0000-000000000002", "00000000-0000-0000-0000-000000000020", "client-2")
        });
    }

    private static ReminderDto CreateReminderDto(
        ReminderKind kind = ReminderKind.Email,
        string recipient = "test@example.com",
        string textBody = "Test reminder")
    {
        return new ReminderDto(
            EventId: Guid.NewGuid(),
            Kind: kind,
            RecipientUserId: "user-123",
            Recipient: recipient,
            Subject: "Test Subject",
            TextBody: textBody,
            RemindAt: DateTimeOffset.UtcNow,
            SentAt: null,
            IsSent: false)
        {
            Id = Guid.NewGuid()
        };
    }

    private static EventDto CreateEventDto()
    {
        return new EventDto(
            ProjectId: Guid.NewGuid(),
            ProcedureId: Guid.NewGuid(),
            Title: "Test Event",
            Description: "Test Description",
            PreviousEventId: null,
            CommittedAt: DateTimeOffset.UtcNow,
            CommittedBy: Guid.NewGuid())
        {
            Id = Guid.NewGuid()
        };
    }
}
