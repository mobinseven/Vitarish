using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Repositories;

namespace Appointments.Tests;

public class ShiftBookableSlotsTests
{
    private static AppointmentsDbContext CreateDbContext()
    {
        DbContextOptions<AppointmentsDbContext> options = new DbContextOptionsBuilder<AppointmentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Mock<IRequestContext> requestContext = new();
        _ = requestContext.SetupGet(x => x.TenantId).Returns("test-tenant");
        return new AppointmentsDbContext(options, [], requestContext.Object);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnEmptyForInvalidDateRange()
    {
        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        Mock<IShiftReservationViewRepository> viewRepo = new();
        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = FutureDate(DayOfWeek.Tuesday).AddDays(7), DateTo = FutureDate(DayOfWeek.Tuesday) };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().BeEmpty();
        shiftRepo.Verify(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnEmptyWhenNoShiftsMatch()
    {
        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<IShiftReservationViewRepository> viewRepo = new();
        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().BeEmpty();
        viewRepo.Verify(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnProjectedSlotsWhenNoExistingReservations()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateActiveShift(DayOfWeek.Tuesday);

        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([shift]);

        Mock<IShiftReservationViewRepository> viewRepo = new();
        _ = viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().HaveCount(1);
        _ = result[0].Shift.Id.Should().Be(shift.Id);
        _ = result[0].Occurrence!.Date.Should().Be(date);
        _ = result[0].Occurrence!.AvailableSeats.Should().Be(shift.TotalSeats);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldReturnSlotsAcrossMultipleShifts()
    {
        DateOnly tuesday = FutureDate(DayOfWeek.Tuesday);
        DateOnly wednesday = FutureDate(DayOfWeek.Wednesday);

        Shift tuesdayShift = CreateActiveShift(DayOfWeek.Tuesday);
        Shift wednesdayShift = CreateActiveShift(DayOfWeek.Wednesday);

        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([tuesdayShift, wednesdayShift]);

        Mock<IShiftReservationViewRepository> viewRepo = new();
        _ = viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        // Range covers both the next Tuesday and the next Wednesday.
        DateOnly rangeStart = tuesday < wednesday ? tuesday : wednesday;
        DateOnly rangeEnd = tuesday > wednesday ? tuesday : wednesday;

        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = rangeStart, DateTo = rangeEnd };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().HaveCount(2);
        _ = result.Should().Contain(s => s.Shift.Id == tuesdayShift.Id && s.Occurrence!.Date == tuesday);
        _ = result.Should().Contain(s => s.Shift.Id == wednesdayShift.Id && s.Occurrence!.Date == wednesday);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldOverlayExistingReservationOccurrence()
    {
        DateOnly date = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateActiveShift(DayOfWeek.Tuesday);

        ShiftReservation existingReservation = new()
        {
            ShiftId = shift.Id,
            Date = date,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            TotalSeats = 10,
            ReservationCount = 3,
            AvailableSeats = 7
        };

        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([shift]);

        Mock<IShiftReservationViewRepository> viewRepo = new();
        _ = viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingReservation]);

        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = date, DateTo = date };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().HaveCount(1);
        _ = result[0].Occurrence!.ReservationCount.Should().Be(3);
        _ = result[0].Occurrence!.AvailableSeats.Should().Be(7);
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldExcludeClosedDates()
    {
        DateOnly closedDate = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateActiveShift(DayOfWeek.Tuesday);

        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([shift]);

        Mock<IShiftReservationViewRepository> viewRepo = new();
        _ = viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using AppointmentsDbContext ctx = CreateDbContext();

        // Seed the closed policy into the in-memory DB
        _ = ctx.ShiftBookingPolicies.Add(new ShiftBookingPolicy
        {
            Id = Guid.NewGuid(),
            ShiftId = shift.Id,
            Date = closedDate,
            Closed = true
        });
        _ = await ctx.SaveChangesAsync();

        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = closedDate, DateTo = closedDate };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookableSlotsAsync_ShouldExcludeShiftsWithNoDatesInRange()
    {
        // A Wednesday shift queried against a range that contains only a Tuesday date.
        DateOnly tuesday = FutureDate(DayOfWeek.Tuesday);
        Shift shift = CreateActiveShift(DayOfWeek.Wednesday);

        Mock<IEntityRepository<Shift, ShiftDescriptor>> shiftRepo = new();
        _ = shiftRepo
            .Setup(r => r.GetAsync(It.IsAny<ShiftDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([shift]);

        Mock<IShiftReservationViewRepository> viewRepo = new();
        _ = viewRepo
            .Setup(r => r.GetAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using AppointmentsDbContext ctx = CreateDbContext();
        ShiftBookableDateQueryService sut = new(shiftRepo.Object, viewRepo.Object, ctx);

        // Range covers only the Tuesday — the Wednesday shift has no slot here.
        ShiftDescriptorDto descriptor = new() { Status = ShiftStatus.Active };
        ShiftDateRangeQuery range = new() { DateFrom = tuesday, DateTo = tuesday };

        IReadOnlyList<BookableShiftSlot> result = await sut.GetBookableSlotsAsync(descriptor, range, CancellationToken.None);

        _ = result.Should().BeEmpty();
    }

    /// <summary>Returns the next occurrence of <paramref name="dayOfWeek"/> that is at least 7 days from now,
    /// ensuring the slot's start time (09:00 UTC) is always in the future when tests run.</summary>
    private static DateOnly FutureDate(DayOfWeek dayOfWeek)
    {
        DateOnly start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        int days = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(days == 0 ? 7 : days);
    }

    private static Shift CreateActiveShift(DayOfWeek dayOfWeek) => new()
    {
        Id = Guid.NewGuid(),
        Status = ShiftStatus.Active,
        DayOfWeek = dayOfWeek,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        TimeZone = "UTC",
        TotalSeats = 10
    };
}
