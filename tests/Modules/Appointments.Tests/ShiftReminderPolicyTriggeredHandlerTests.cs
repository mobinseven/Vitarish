using Vitarish.Appointments.Infrastructure.Handlers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Appointments.Tests;

public class ShiftReminderPolicyTriggeredHandlerTests
{
    private readonly Mock<IEntityRepository<Shift, ShiftDescriptor>> _shiftRepository = new();
    private readonly Mock<IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor>> _reminderPolicyRepository = new();
    private readonly Mock<IRemindersService> _remindersService = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly TestTimeProvider _timeProvider = new();

    private ShiftReminderPolicyTriggeredHandler CreateSut()
        => new(_shiftRepository.Object, _reminderPolicyRepository.Object, _remindersService.Object, _mediator.Object, _timeProvider);

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenNoPoliciesExist()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        (Shift? shift, _, DateOnly date) = CreateShift();

        _ = _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        SetupPolicies(shift.Id, []);

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenStatusDoesNotQualify()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        ShiftReminderPolicy policy = CreatePolicy([ReservationStatus.Confirmed]);
        (Shift? shift, _, DateOnly date) = CreateShift();

        _ = _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        policy.ShiftId = shift.Id;
        SetupPolicies(shift.Id, [policy]);

        // Event has Pending status, policy qualifies only Confirmed
        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateReminder_WhenRemindAtIsInThePast()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        // Shift starts 30 minutes in the future, offset 1 hour → remindAt = 30 min in the past
        DateTimeOffset startTime = _timeProvider.UtcNow.AddMinutes(30);
        ShiftReminderPolicy policy = CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1));
        (Shift? shift, _, DateOnly date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _ = _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        policy.ShiftId = shift.Id;
        SetupPolicies(shift.Id, [policy]);

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateOneReminderPerMatchingPolicy()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        DateTimeOffset startTime = _timeProvider.UtcNow.AddDays(2);
        ShiftReminderPolicy[] policiesList = new[]
        {
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(24)),
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1))
        };
        (Shift? shift, _, DateOnly date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _ = _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        foreach (ShiftReminderPolicy? p in policiesList)
        {
            p.ShiftId = shift.Id;
        }

        SetupPolicies(shift.Id, policiesList);

        User user = new()
        { Email = "user@example.com", UserName = "user" };
        _ = _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _ = _remindersService.Setup(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending, "user-1"), CancellationToken.None);

        _remindersService.Verify(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSkipReminder_WhenReservingPartyIdIsNull()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        (Shift? shift, Guid _, DateOnly date) = CreateShift();

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending, reservingPartyId: null), CancellationToken.None);

        _shiftRepository.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRenderTemplateTokens()
    {
        ShiftReminderPolicyTriggeredHandler sut = CreateSut();
        DateTimeOffset startTime = _timeProvider.UtcNow.AddDays(2);
        ShiftReminderPolicy policy = CreatePolicy(
            [ReservationStatus.Confirmed],
            subjectTemplate: "Appointment at {{OccurrenceStart}}",
            bodyTemplate: "Your shift starts at {{OccurrenceStart}}.");
        (Shift? shift, _, DateOnly date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _ = _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        policy.ShiftId = shift.Id;
        SetupPolicies(shift.Id, [policy]);

        User user = new()
        { Email = "user@example.com", UserName = "user" };
        _ = _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        string? capturedSubject = null;
        string? capturedBody = null;
        _ = _remindersService.Setup(r => r.AddRangeAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ReminderRecipientReadDto>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyList<ReminderRecipientReadDto>, string, string, DateTimeOffset, CancellationToken>(
                (_, _, subject, body, _, _) => { capturedSubject = subject; capturedBody = body; })
            .ReturnsAsync([]);

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Confirmed, "user-1"), CancellationToken.None);

        _ = capturedSubject.Should().NotBeNull();
        _ = capturedSubject.Should().NotContain("{{OccurrenceStart}}");
        _ = capturedSubject.Should().Contain("Appointment at");
        _ = capturedBody.Should().NotContain("{{OccurrenceStart}}");
        _ = capturedBody.Should().Contain("Your shift starts at");
    }

    private void SetupPolicies(Guid shiftId, IReadOnlyList<ShiftReminderPolicy> policies) => _reminderPolicyRepository.Setup(r => r.GetAsync(
                                                                                                      It.Is<ShiftReminderPolicyDescriptor>(d => d.ShiftId == shiftId && d.IsEnabled == true),
                                                                                                      It.IsAny<CancellationToken>()))
            .ReturnsAsync(policies);

    private (Shift Shift, Guid ShiftId, DateOnly Date) CreateShift(TimeOnly? startTime = null)
    {
        Guid shiftId = Guid.NewGuid();
        TimeOnly resolvedStart = startTime ?? TimeOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);
        DateOnly date = DateOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);

        Shift shift = new()
        {
            Id = shiftId,
            StartTime = resolvedStart,
            EndTime = resolvedStart.AddHours(1)
        };

        return (shift, shiftId, date);
    }

    private static ShiftReminderPolicy CreatePolicy(
        IList<ReservationStatus> statuses,
        TimeSpan? offset = null,
        string subjectTemplate = "Reminder: {{OccurrenceStart}}",
        string bodyTemplate = "Your appointment is at {{OccurrenceStart}}.")
    {
        ShiftReminderPolicy policy = new()
        {
            Id = Guid.NewGuid(),
            IsEnabled = true,
            Channel = ReminderKind.Email,
            OffsetBefore = offset ?? TimeSpan.FromHours(24),
            SubjectTemplate = subjectTemplate,
            BodyTemplate = bodyTemplate,
            QualifyingStatuses = statuses
        };
        return policy;
    }

    private static ShiftReminderPolicyTriggeredEvent CreateEvent(
        Guid shiftId,
        DateOnly date,
        ReservationStatus status,
        string? reservingPartyId = "user-1")
        => new(Guid.NewGuid(), shiftId, date, reservingPartyId, status);

    private sealed class TestTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
