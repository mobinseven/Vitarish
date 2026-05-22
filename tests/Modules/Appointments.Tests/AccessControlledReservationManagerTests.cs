namespace Appointments.Tests;

public class AccessControlledReservationManagerTests
{
    [Fact]
    public async Task ReserveAsync_ShouldConfirmReservationWhenBookingIsOpen()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IShiftBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid shiftId = Guid.NewGuid();
        Guid reservationId = Guid.NewGuid();

        DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddHours(2);
        DateOnly date = DateOnly.FromDateTime(slotStart.DateTime);

        bookingPolicyEngine.Setup(x => x.IsBookingOpenAsync(shiftId, date, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        bookingPolicyEngine.Setup(x => x.HasCapacityForPartyAsync(shiftId, date, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reservationManager
            .Setup(x => x.ReserveAsync(shiftId, date, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                ShiftId = shiftId,
                Date = date,
                ReservingPartyId = "user-1",
                Status = ReservationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }));

        reservationManager
            .Setup(x => x.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success([]));

        reservationManager
            .Setup(x => x.GetAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                ShiftId = shiftId,
                Date = date,
                ReservingPartyId = "user-1",
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTimeOffset.UtcNow
            }));

        statusCoordinator
            .Setup(x => x.TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());

        bookingPolicyEngine
            .Setup(x => x.RequiresApprovalAsync(shiftId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            bookingPolicyEngine.Object,
            statusCoordinator.Object);

        EntityResult<ReservationReadDto> result = await sut.ReserveAsync(shiftId, date, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservationManager.Verify(x => x.GetListAsync(
            It.Is<ReservationDescriptorDto>(descriptor =>
                descriptor.ShiftId == shiftId &&
                descriptor.DateFrom == date &&
                descriptor.DateTo == date &&
                descriptor.ReservingPartyId == "user-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        reservationManager.Verify(x => x.ReserveAsync(shiftId, date, "user-1", It.IsAny<CancellationToken>()), Times.Once);
        statusCoordinator.Verify(x => x.TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, "user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_ShouldScopeNonAdminDescriptorAndPreserveDateRange()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IShiftBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid shiftId = Guid.NewGuid();
        DateOnly dateFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        DateOnly dateTo = dateFrom.AddDays(7);
        ReservationDescriptorDto descriptor = new()
        {
            ShiftId = shiftId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            ReservingPartyId = "other-user"
        };

        reservationManager
            .Setup(x => x.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success([]));

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            bookingPolicyEngine.Object,
            statusCoordinator.Object);

        EntityResult<IReadOnlyList<ReservationReadDto>> result = await sut.GetListAsync(descriptor, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservationManager.Verify(x => x.GetListAsync(
            It.Is<ReservationDescriptorDto>(scopedDescriptor =>
                scopedDescriptor.ShiftId == shiftId &&
                scopedDescriptor.DateFrom == dateFrom &&
                scopedDescriptor.DateTo == dateTo &&
                scopedDescriptor.ReservingPartyId == "user-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReserveWithOverrideAsync_ShouldRequireElevatedAccess()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IShiftBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            bookingPolicyEngine.Object,
            statusCoordinator.Object);

        Func<Task> act = () => sut.ReserveWithOverrideAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Elevated access is required to perform this operation.");
    }

    [Fact]
    public async Task ReserveWithOverrideAsync_ShouldUseOverridePathForOwner()
    {
        Mock<IRequestContext> requestContext = CreateOwnerRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IShiftBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid shiftId = Guid.NewGuid();
        Guid reservationId = Guid.NewGuid();
        DateOnly date = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        bookingPolicyEngine.Setup(x => x.IsBookingOpenAsync(shiftId, date, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        bookingPolicyEngine.Setup(x => x.RequiresApprovalAsync(shiftId, date, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reservationManager
            .Setup(x => x.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success([]));
        reservationManager
            .Setup(x => x.ReserveWithOverrideAsync(shiftId, date, "patient-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                ShiftId = shiftId,
                Date = date,
                ReservingPartyId = "patient-1",
                Status = ReservationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }));
        reservationManager
            .Setup(x => x.GetAsync(reservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                ShiftId = shiftId,
                Date = date,
                ReservingPartyId = "patient-1",
                Status = ReservationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }));

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            bookingPolicyEngine.Object,
            statusCoordinator.Object);

        EntityResult<ReservationReadDto> result = await sut.ReserveWithOverrideAsync(shiftId, date, "patient-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        bookingPolicyEngine.Verify(x => x.HasCapacityForPartyAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        reservationManager.Verify(x => x.ReserveWithOverrideAsync(shiftId, date, "patient-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IRequestContext> CreateRequestContext()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        requestContext.SetupGet(x => x.UserId).Returns("user-1");
        requestContext.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        return requestContext;
    }

    private static Mock<IRequestContext> CreateOwnerRequestContext()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        requestContext.SetupGet(x => x.UserId).Returns("owner-1");
        requestContext.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "tenant-a:TenantOwner")
        ], "test")));
        return requestContext;
    }
}
