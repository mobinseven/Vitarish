namespace Appointments.Tests;

public class AccessControlledReservationManagerTests
{
    [Fact]
    public async Task ReserveAsync_ShouldConfirmReservationWhenBookingIsOpen()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<ISessionBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid sessionId = Guid.NewGuid();
        Guid reservationId = Guid.NewGuid();

        DateTimeOffset slotStart = DateTimeOffset.UtcNow.AddHours(2);
        DateOnly date = DateOnly.FromDateTime(slotStart.DateTime);

        bookingPolicyEngine.Setup(x => x.IsBookingOpenAsync(sessionId, date, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        bookingPolicyEngine.Setup(x => x.HasCapacityForPartyAsync(sessionId, date, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reservationManager
            .Setup(x => x.ReserveAsync(sessionId, date, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                SessionId = sessionId,
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
                SessionId = sessionId,
                Date = date,
                ReservingPartyId = "user-1",
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTimeOffset.UtcNow
            }));

        statusCoordinator
            .Setup(x => x.TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            bookingPolicyEngine.Object,
            statusCoordinator.Object);

        EntityResult<ReservationReadDto> result = await sut.ReserveAsync(sessionId, date, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservationManager.Verify(x => x.ReserveAsync(sessionId, date, "user-1", It.IsAny<CancellationToken>()), Times.Once);
        statusCoordinator.Verify(x => x.TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, "user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IRequestContext> CreateRequestContext()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(x => x.TenantId).Returns("tenant-a");
        requestContext.SetupGet(x => x.UserId).Returns("user-1");
        requestContext.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        return requestContext;
    }
}
