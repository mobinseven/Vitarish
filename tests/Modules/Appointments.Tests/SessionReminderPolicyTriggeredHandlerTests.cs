using MediatR;
using Vitarish.Appointments.Infrastructure.Handlers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Appointments.Tests;

public class SessionReminderPolicyTriggeredHandlerTests
{
    private readonly Mock<ISessionReminderPolicyRepository> _policyRepository = new();
    private readonly Mock<IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>> _occurrenceRepository = new();
    private readonly Mock<IEntityRepository<Session, SessionDescriptor>> _sessionRepository = new();
    private readonly Mock<IRemindersService> _remindersService = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly TestTimeProvider _timeProvider = new();

    private SessionReminderPolicyTriggeredHandler CreateSut()
        => new(_policyRepository.Object, _occurrenceRepository.Object, _sessionRepository.Object,
               _remindersService.Object, _mediator.Object, _timeProvider);

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenNoPoliciesExist()
    {
        var sut = CreateSut();
        var occurrence = CreateOccurrence();

        _occurrenceRepository.Setup(r => r.GetAsync(occurrence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occurrence);
        _policyRepository.Setup(r => r.GetEnabledBySessionIdAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenStatusDoesNotQualify()
    {
        var sut = CreateSut();
        var occurrence = CreateOccurrence();
        var session = new Session { Id = occurrence.SessionId, Title = "Test Session" };

        _occurrenceRepository.Setup(r => r.GetAsync(occurrence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occurrence);
        _sessionRepository.Setup(r => r.GetAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _policyRepository.Setup(r => r.GetEnabledBySessionIdAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePolicy(occurrence.SessionId, [ReservationStatus.Confirmed])]);

        // Event has Pending status, policy qualifies only Confirmed
        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenRemindAtIsInThePast()
    {
        var sut = CreateSut();
        // OccurrenceStart 30 minutes in the future, offset 1 hour → remindAt = 30 min in the past
        var occurrenceStart = _timeProvider.UtcNow.AddMinutes(30);
        var occurrence = CreateOccurrence(start: occurrenceStart);
        var session = new Session { Id = occurrence.SessionId, Title = "Test Session" };
        var policy = CreatePolicy(occurrence.SessionId, [ReservationStatus.Pending], offset: TimeSpan.FromHours(1));

        _occurrenceRepository.Setup(r => r.GetAsync(occurrence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occurrence);
        _sessionRepository.Setup(r => r.GetAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _policyRepository.Setup(r => r.GetEnabledBySessionIdAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([policy]);

        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateOneReminderPerMatchingPolicy()
    {
        var sut = CreateSut();
        var occurrenceStart = _timeProvider.UtcNow.AddDays(2);
        var occurrence = CreateOccurrence(start: occurrenceStart);
        var session = new Session { Id = occurrence.SessionId, Title = "Test Session" };
        var policy1 = CreatePolicy(occurrence.SessionId, [ReservationStatus.Pending], offset: TimeSpan.FromHours(24));
        var policy2 = CreatePolicy(occurrence.SessionId, [ReservationStatus.Pending], offset: TimeSpan.FromHours(1));

        _occurrenceRepository.Setup(r => r.GetAsync(occurrence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occurrence);
        _sessionRepository.Setup(r => r.GetAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _policyRepository.Setup(r => r.GetEnabledBySessionIdAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([policy1, policy2]);

        var user = new User { Email = "user@example.com", UserName = "user" };
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _remindersService.Setup(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Pending, "user-1"), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSkipReminder_WhenReservingPartyIdIsNull()
    {
        var sut = CreateSut();
        var occurrence = CreateOccurrence();

        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Pending, reservingPartyId: null), CancellationToken.None);

        _policyRepository.Verify(r => r.GetEnabledBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRenderTemplateTokens()
    {
        var sut = CreateSut();
        var occurrenceStart = _timeProvider.UtcNow.AddDays(2);
        var occurrence = CreateOccurrence(start: occurrenceStart);
        var session = new Session { Id = occurrence.SessionId, Title = "My Session" };
        var policy = CreatePolicy(occurrence.SessionId, [ReservationStatus.Confirmed],
            subjectTemplate: "Appointment: {{SessionTitle}} at {{OccurrenceStart}}",
            bodyTemplate: "Your session {{SessionTitle}} starts at {{OccurrenceStart}}.");

        _occurrenceRepository.Setup(r => r.GetAsync(occurrence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(occurrence);
        _sessionRepository.Setup(r => r.GetAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _policyRepository.Setup(r => r.GetEnabledBySessionIdAsync(occurrence.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([policy]);

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

        await sut.Handle(CreateEvent(occurrence, ReservationStatus.Confirmed, "user-1"), CancellationToken.None);

        capturedSubject.Should().NotBeNull();
        capturedSubject.Should().NotContain("{{SessionTitle}}");
        capturedSubject.Should().NotContain("{{OccurrenceStart}}");
        capturedSubject.Should().Contain("My Session");
        capturedBody.Should().NotContain("{{SessionTitle}}");
        capturedBody.Should().NotContain("{{OccurrenceStart}}");
        capturedBody.Should().Contain("My Session");
    }

    private static SessionOccurrence CreateOccurrence(DateTimeOffset? start = null)
    {
        return new SessionOccurrence
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            OccurrenceStart = start ?? DateTimeOffset.UtcNow.AddDays(3),
            OccurrenceEnd = (start ?? DateTimeOffset.UtcNow.AddDays(3)).AddHours(1),
            RecurrenceId = "rec-1",
            CalendarMappingId = Guid.NewGuid()
        };
    }

    private static SessionReminderPolicy CreatePolicy(
        Guid sessionId,
        IList<ReservationStatus> statuses,
        TimeSpan? offset = null,
        string subjectTemplate = "Reminder: {{SessionTitle}} {{OccurrenceStart}}",
        string bodyTemplate = "Your appointment {{SessionTitle}} is at {{OccurrenceStart}}.")
    {
        var policy = new SessionReminderPolicy
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
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
        SessionOccurrence occurrence,
        ReservationStatus status,
        string? reservingPartyId = "user-1")
        => new(
            Guid.NewGuid(),
            occurrence.Id,
            reservingPartyId,
            status,
            occurrence.OccurrenceStart);

    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
