using Vitarish.Appointments.Infrastructure.Repositories;

namespace Appointments.Tests;

public class SessionBookableSlotsTests
{
    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnEmptyForInvalidDateRange()
    {
        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        Mock<ISessionReservationViewRepository> viewRepo = new();
        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = FutureDate(DayOfWeek.Tuesday).AddDays(7), DateTo = FutureDate(DayOfWeek.Tuesday) };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().BeEmpty();
        sessionRepo.Verify(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnEmptyWhenNoSessionsMatch()
    {
        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<ISessionReservationViewRepository> viewRepo = new();
        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().BeEmpty();
        viewRepo.Verify(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnProjectedSlotsWhenNoExistingReservations()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Session session = CreateActiveSession(DayOfWeek.Tuesday);

        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        Mock<ISessionReservationViewRepository> viewRepo = new();
        viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Session.Id.Should().Be(session.Id);
        result[0].Occurrence!.Date.Should().Be(date);
        result[0].Occurrence!.AvailableSeats.Should().Be(session.TotalSeats);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnSlotsAcrossMultipleSessions()
    {
        DateOnly tuesday = FutureDate(DayOfWeek.Tuesday);
        DateOnly wednesday = FutureDate(DayOfWeek.Wednesday);

        Session tuesdaySession = CreateActiveSession(DayOfWeek.Tuesday);
        Session wednesdaySession = CreateActiveSession(DayOfWeek.Wednesday);

        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([tuesdaySession, wednesdaySession]);

        Mock<ISessionReservationViewRepository> viewRepo = new();
        viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        // Range covers both the next Tuesday and the next Wednesday.
        DateOnly rangeStart = tuesday < wednesday ? tuesday : wednesday;
        DateOnly rangeEnd = tuesday > wednesday ? tuesday : wednesday;

        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = rangeStart, DateTo = rangeEnd };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Session.Id == tuesdaySession.Id && s.Occurrence!.Date == tuesday);
        result.Should().Contain(s => s.Session.Id == wednesdaySession.Id && s.Occurrence!.Date == wednesday);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldOverlayExistingReservationOccurrence()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Session session = CreateActiveSession(DayOfWeek.Tuesday);

        SessionReservation existingReservation = new()
        {
            SessionId = session.Id,
            Date = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            TotalSeats = 10,
            ReservationCount = 3,
            AvailableSeats = 7
        };

        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        Mock<ISessionReservationViewRepository> viewRepo = new();
        viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingReservation]);

        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Occurrence!.ReservationCount.Should().Be(3);
        result[0].Occurrence!.AvailableSeats.Should().Be(7);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldExcludeClosedDates()
    {
        DateOnly closedDate = FutureDate(DayOfWeek.Tuesday);
        Session session = CreateActiveSession(DayOfWeek.Tuesday);
        session.BookingPolicy = new SessionBookingPolicy
        {
            ClosedDates = [new ClosedDate(closedDate, "Test closure")]
        };

        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        Mock<ISessionReservationViewRepository> viewRepo = new();
        viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = closedDate, DateTo = closedDate };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldExcludeSessionsWithNoDatesInRange()
    {
        // A Wednesday session queried against a range that contains only a Tuesday date.
        DateOnly tuesday = FutureDate(DayOfWeek.Tuesday);
        Session session = CreateActiveSession(DayOfWeek.Wednesday);

        Mock<IEntityRepository<Session, SessionDescriptor>> sessionRepo = new();
        sessionRepo
            .Setup(r => r.GetAsync(It.IsAny<SessionDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        Mock<ISessionReservationViewRepository> viewRepo = new();
        viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        SessionBookableDateQueryService sut = new(sessionRepo.Object, viewRepo.Object);

        // Range covers only the Tuesday — the Wednesday session has no slot here.
        SessionDescriptorDto descriptor = new() { Status = SessionStatus.Active };
        SessionDateRangeQuery range = new() { DateFrom = tuesday, DateTo = tuesday };

        IReadOnlyList<BookableSessionSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Returns the next occurrence of <paramref name="dayOfWeek"/> that is at least 7 days from now,
    /// ensuring the slot's start time (09:00 UTC) is always in the future when tests run.</summary>
    private static DateOnly FutureDate(DayOfWeek dayOfWeek)
    {
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        int days = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(days == 0 ? 7 : days);
    }

    private static Session CreateActiveSession(DayOfWeek dayOfWeek) => new()
    {
        Id = Guid.NewGuid(),
        Status = SessionStatus.Active,
        DayOfWeek = dayOfWeek,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        TimeZone = "UTC",
        TotalSeats = 10
    };
}
