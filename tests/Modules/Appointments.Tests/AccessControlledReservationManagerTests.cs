namespace Appointments.Tests;

public class AccessControlledReservationManagerTests
{
    [Fact]
    public async Task ReserveAsync_ShouldConfirmReservationWhenApprovalIsNotRequired()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>> occurrenceRepository = new();
        Mock<IBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<ICapacityEngine> capacityEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid occurrenceId = Guid.NewGuid();
        Guid reservationId = Guid.NewGuid();

        occurrenceRepository
            .Setup(x => x.GetAsync(occurrenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionOccurrence
            {
                Id = occurrenceId,
                OccurrenceStart = DateTimeOffset.UtcNow.AddHours(2),
                OccurrenceEnd = DateTimeOffset.UtcNow.AddHours(3),
                Session = new Session
                {
                    Id = Guid.NewGuid(),
                    Status = SessionStatus.Active,
                    RequiresApproval = false,
                    TotalSeats = 2,
                    StartAt = DateTimeOffset.UtcNow.AddHours(2),
                    EndAt = DateTimeOffset.UtcNow.AddHours(3),
                    TimeZone = TimeZoneInfo.Utc.Id
                }
            });

        bookingPolicyEngine.Setup(x => x.IsBookingOpenAsync(occurrenceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        bookingPolicyEngine.Setup(x => x.HasCapacityForPartyAsync(occurrenceId, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        capacityEngine.Setup(x => x.IsAvailableAsync(occurrenceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reservationManager
            .Setup(x => x.ReserveAsync(occurrenceId, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                SessionOccurrenceId = occurrenceId,
                ReservingPartyId = "user-1",
                Status = ReservationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }));

        reservationManager
            .Setup(x => x.GetListAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<ReservationReadDto>>.Success([]));

        statusCoordinator
            .Setup(x => x.TransitionReservationAsync(reservationId, ReservationStatus.Confirmed, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult.Success());

        AccessControlledReservationManager sut = new(
            requestContext.Object,
            reservationManager.Object,
            occurrenceRepository.Object,
            bookingPolicyEngine.Object,
            capacityEngine.Object,
            statusCoordinator.Object);

        EntityResult<ReservationReadDto> result = await sut.ReserveAsync(occurrenceId, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservationManager.Verify(x => x.ReserveAsync(occurrenceId, "user-1", It.IsAny<CancellationToken>()), Times.Once);
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
