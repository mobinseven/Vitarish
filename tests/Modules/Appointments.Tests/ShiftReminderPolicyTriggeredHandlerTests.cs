using MediatR;
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
        var sut = CreateSut();
        var (shift, _, date) = CreateShift();

        _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
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
        var sut = CreateSut();
        var policy = CreatePolicy([ReservationStatus.Confirmed]);
        var (shift, _, date) = CreateShift();

        _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
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
        var sut = CreateSut();
        // Shift starts 30 minutes in the future, offset 1 hour → remindAt = 30 min in the past
        var startTime = _timeProvider.UtcNow.AddMinutes(30);
        var policy = CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1));
        var (shift, _, date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
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
        var sut = CreateSut();
        var startTime = _timeProvider.UtcNow.AddDays(2);
        var policiesList = new[]
        {
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(24)),
            CreatePolicy([ReservationStatus.Pending], offset: TimeSpan.FromHours(1))
        };
        var (shift, _, date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        foreach (var p in policiesList) p.ShiftId = shift.Id;
        SetupPolicies(shift.Id, policiesList);

        var user = new User { Email = "user@example.com", UserName = "user" };
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _remindersService.Setup(r => r.AddRangeAsync(
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
        var sut = CreateSut();
        var (shift, _, date) = CreateShift();

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Pending, reservingPartyId: null), CancellationToken.None);

        _shiftRepository.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRenderTemplateTokens()
    {
        var sut = CreateSut();
        var startTime = _timeProvider.UtcNow.AddDays(2);
        var policy = CreatePolicy(
            [ReservationStatus.Confirmed],
            subjectTemplate: "Appointment at {{OccurrenceStart}}",
            bodyTemplate: "Your shift starts at {{OccurrenceStart}}.");
        var (shift, _, date) = CreateShift(startTime: TimeOnly.FromDateTime(startTime.DateTime));

        _shiftRepository.Setup(r => r.GetAsync(shift.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shift);
        policy.ShiftId = shift.Id;
        SetupPolicies(shift.Id, [policy]);

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

        await sut.Handle(CreateEvent(shift.Id, date, ReservationStatus.Confirmed, "user-1"), CancellationToken.None);

        capturedSubject.Should().NotBeNull();
        capturedSubject.Should().NotContain("{{OccurrenceStart}}");
        capturedSubject.Should().Contain("Appointment at");
        capturedBody.Should().NotContain("{{OccurrenceStart}}");
        capturedBody.Should().Contain("Your shift starts at");
    }

    private void SetupPolicies(Guid shiftId, IReadOnlyList<ShiftReminderPolicy> policies)
    {
        _reminderPolicyRepository.Setup(r => r.GetAsync(
            It.Is<ShiftReminderPolicyDescriptor>(d => d.ShiftId == shiftId && d.IsEnabled == true),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(policies);
    }

    private (Shift Shift, Guid ShiftId, DateOnly Date) CreateShift(TimeOnly? startTime = null)
    {
        var shiftId = Guid.NewGuid();
        var resolvedStart = startTime ?? TimeOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);
        var date = DateOnly.FromDateTime(_timeProvider.UtcNow.AddDays(3).DateTime);

        var shift = new Shift
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
        var policy = new ShiftReminderPolicy
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
