namespace Appointments.Tests;

public class AccessControlledShiftManagerTests
{
    [Fact]
    public async Task AddAsync_ShouldRejectShiftsWithInvalidTimeRange()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext(isAdmin: true);
        Mock<IShiftManager> shiftManager = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        AccessControlledShiftManager sut = new(
            requestContext.Object,
            shiftManager.Object,
            statusCoordinator.Object);

        ShiftWriteDto dto = new()
        {
            TimeZone = TimeZoneInfo.Utc.Id,
            DayOfWeek = System.DayOfWeek.Monday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(9, 0),
            TotalSeats = 1
        };

        EntityResult<ShiftReadDto> result = await sut.AddAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("end time", StringComparison.OrdinalIgnoreCase));
        shiftManager.Verify(m => m.AddAsync(It.IsAny<ShiftWriteDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IRequestContext> CreateRequestContext(bool isAdmin)
    {
        const string tenantId = "tenant-a";
        List<Claim> claims =
        [
            new(IdentityDefinitions.TenantIdClaim, tenantId)
        ];

        if (isAdmin)
        {
            claims.Add(new(ClaimTypes.Role, $"{tenantId}:{IdentityDefinitions.TenantAdmin}"));
        }

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(x => x.TenantId).Returns(tenantId);
        requestContext.SetupGet(x => x.UserId).Returns("user-1");
        requestContext.SetupGet(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
        return requestContext;
    }
}
