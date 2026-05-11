using MediatR;
using Vitarish.Appointments.Infrastructure.Handlers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Appointments.Tests;

public class SessionReminderPolicyTriggeredHandlerTests
{
    private readonly Mock<IEntityRepository<Session, SessionDescriptor>> _sessionRepository = new();
    private readonly Mock<IRemindersService> _remindersService = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly TestTimeProvider _timeProvider = new();

    private SessionReminderPolicyTriggeredHandler CreateSut()
        => new(_sessionRepository.Object, _remindersService.Object, _mediator.Object, _timeProvider);

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenNoPoliciesExist()
    {
        var sut = CreateSut();
        var (session, _, date) = CreateSession();

        _sessionRepository.Setup(r => r.GetAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenStatusDoesNotQualify()
    {
        var sut = CreateSut();
        var (session, _, date) = CreateSession(policies: [CreatePolicy([ReservationStatus.Confirmed])]);

        _sessionRepository.Setup(r => r.GetAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Event has Pending status, policy qualifies only Confirmed
        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenRemindAtIsInThePast()
    {
        var sut = CreateSut();
        // Session starts 30 minutes in the future, offset 1 hour → remindAt = 30 min in the past
        var startTime = _timeProvider.UtcNow.AddMinutes(30);
        var (session, _, date) = CreateSession(
            startTime: TimeOnly.FromDateTime(startTime.DateTime),
            policies: [CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1))]);

        _sessionRepository.Setup(r => r.GetAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateOneReminderPerMatchingPolicy()
    {
        var sut = CreateSut();
        var startTime = _timeProvider.UtcNow.AddDays(2);
        var policies = new[]
        {
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(24)),
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1))
        };
        var (session, _, date) = CreateSession(startTime: TimeOnly.FromDateTime(startTime.DateTime), policies: policies);

        _sessionRepository.Setup(r => r.GetAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var user = new User { Email = "user@example.com", UserName = "user" };
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _remindersService.Setup(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Pending, "user-1"), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSkipReminder_WhenReservingPartyIdIsNull()
    {
        var sut = CreateSut();
        var (session, _, date) = CreateSession();

        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Pending, reservingPartyId: null), CancellationToken.None);

        _sessionRepository.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRenderTemplateTokens()
    {
        var sut = CreateSut();
        var startTime = _timeProvider.UtcNow.AddDays(2);
        var policies = new[]
        {
            CreatePolicy(
                [ReservationStatus.Confirmed],
                subjectTemplate: "Appointment: {{SessionTitle}} at {{OccurrenceStart}}",
                bodyTemplate: "Your session {{SessionTitle}} starts at {{OccurrenceStart}}.")
        };
        var (session, _, date) = CreateSession(
            title: "My Session",
            startTime: TimeOnly.FromDateTime(startTime.DateTime),
            policies: policies);

        _sessionRepository.Setup(r => r.GetAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var user = new User { Email = "user@example.com", UserName = "user" };
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        string? capturedSubject = null;
        string? capturedBody = null;
        _remindersService.Setup(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<ReminderRecipientReadDto>, string, string, DateTimeOffset, CancellationToken>(
                (_, _, subject, body, _, _) => { capturedSubject = subject; capturedBody = body; })
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(session.Id, date, ReservationStatus.Confirmed, "user-1"), CancellationToken.None);

        capturedSubject.Should().NotBeNull();
        capturedSubject.Should().NotContain("{{SessionTitle}}");
        capturedSubject.Should().NotContain("{{OccurrenceStart}}");
        capturedSubject.Should().Contain("My Session");
        capturedBody.Should().NotContain("{{SessionTitle}}");
        capturedBody.Should().NotContain("{{OccurrenceStart}}");
        capturedBody.Should().Contain("My Session");
    }

    private (Session Session, Guid SessionId, DateOnly Date) CreateSession(
        string title = "Test Session",
        TimeOnly? startTime = null,
        IEnumerable<SessionReminderPolicy>? policies = null)
    {
        var sessionId = Guid.NewGuid();
        var resolvedStart = startTime ?? TimeOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);
        var date = DateOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);

        var session = new Session
        {
            Id = sessionId,
            Title = title,
            StartTime = resolvedStart,
            EndTime = resolvedStart.AddHours(1)
        };

        if (policies is not null)
        {
            foreach (var p in policies)
            {
                p.SessionId = sessionId;
                session.ReminderPolicies.Add(p);
            }
        }

        return (session, sessionId, date);
    }

    private static SessionReminderPolicy CreatePolicy(
        IList<ReservationStatus> statuses,
        TimeSpan? offset = null,
        string subjectTemplate = "Reminder: {{SessionTitle}} {{OccurrenceStart}}",
        string bodyTemplate = "Your appointment {{SessionTitle}} is at {{OccurrenceStart}}.")
    {
        var policy = new SessionReminderPolicy
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            Channel = ReminderKind.Email,
            OffsetBefore = offset ?? TimeSpan.FromHours(24),
            SubjectTemplate = subjectTemplate,
            BodyTemplate = bodyTemplate
        };
        policy.QualifyingStatuses = statuses;
        return policy;
    }

    private static SessionReminderPolicyTriggeredEvent CreateEvent(
        Guid sessionId,
        DateOnly date,
        ReservationStatus status,
        string? reservingPartyId = "user-1")
        => new(Guid.NewGuid(), sessionId, date, reservingPartyId, status);

    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
