namespace Appointments.Tests;

public class AccessControlledReservationManagerTests
{
    [Fact]
    public async Task ReserveAsync_ShouldConfirmReservationWhenApprovalIsNotRequired()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext();
        Mock<IReservationManager> reservationManager = new();
        Mock<IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor>> occurrenceRepository = new();
        Mock<ISessionOccurrenceFactory> occurrenceFactory = new();
        Mock<IBookingPolicyEngine> bookingPolicyEngine = new();
        Mock<ICapacityEngine> capacityEngine = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        Guid sessionId = Guid.NewGuid();
        Guid occurrenceId = Guid.NewGuid();
        Guid reservationId = Guid.NewGuid();

        DateTimeOffset occurrenceStart = DateTimeOffset.UtcNow.AddHours(2);
        DateTimeOffset occurrenceEnd = DateTimeOffset.UtcNow.AddHours(3);
        DateOnly date = DateOnly.FromDateTime(occurrenceStart.DateTime);

        occurrenceFactory
            .Setup(x => x.GetOrCreateAsync(sessionId, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<Guid>.Success(occurrenceId));

        occurrenceRepository
            .Setup(x => x.GetAsync(occurrenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SessionOccurrence
            {
                Id = occurrenceId,
                Date = DateOnly.FromDateTime(occurrenceStart.DateTime),
                StartTime = TimeOnly.FromDateTime(occurrenceStart.DateTime),
                EndTime = TimeOnly.FromDateTime(occurrenceEnd.DateTime),
                Capacity = 2,
                Session = new Session
                {
                    Id = sessionId,
                    Status = SessionStatus.Active,
                    RequiresApproval = false,
                    TotalSeats = 2,
                    DayOfWeek = date.DayOfWeek,
                    StartTime = TimeOnly.FromDateTime(occurrenceStart.DateTime),
                    EndTime = TimeOnly.FromDateTime(occurrenceEnd.DateTime),
                    TimeZone = TimeZoneInfo.Utc.Id
                }
            });

        bookingPolicyEngine.Setup(x => x.IsBookingOpenAsync(occurrenceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        bookingPolicyEngine.Setup(x => x.HasCapacityForPartyAsync(occurrenceId, "user-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        capacityEngine.Setup(x => x.IsAvailableAsync(occurrenceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        reservationManager
            .Setup(x => x.ReserveAsync(sessionId, date, "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ReservationReadDto>.Success(new ReservationReadDto
            {
                Id = reservationId,
                SessionOccurrenceId = occurrenceId,
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
                SessionOccurrenceId = occurrenceId,
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
            occurrenceRepository.Object,
            occurrenceFactory.Object,
            bookingPolicyEngine.Object,
            capacityEngine.Object,
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
