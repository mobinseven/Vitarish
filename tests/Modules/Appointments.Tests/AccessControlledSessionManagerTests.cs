namespace Appointments.Tests;

public class AccessControlledSessionManagerTests
{
    [Fact]
    public async Task AddAsync_ShouldRejectSessionsWithoutSchedule()
    {
        Mock<IRequestContext> requestContext = CreateRequestContext(isAdmin: true);
        Mock<ISessionManager> sessionManager = new();
        Mock<ISessionOccurrenceSyncQueue> syncQueue = new();
        Mock<IStatusCoordinator> statusCoordinator = new();

        AccessControlledSessionManager sut = new(
            requestContext.Object,
            sessionManager.Object,
            syncQueue.Object,
            statusCoordinator.Object);

        SessionWriteDto dto = new()
        {
            Title = "Missing schedule",
            TimeZone = TimeZoneInfo.Utc.Id,
            TotalSeats = 1
        };

        EntityResult<SessionReadDto> result = await sut.AddAsync(dto, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("start and end time", StringComparison.OrdinalIgnoreCase));
        sessionManager.Verify(m => m.AddAsync(It.IsAny<SessionWriteDto>(), It.IsAny<CancellationToken>()), Times.Never);
        syncQueue.Verify(m => m.EnqueueSync(It.IsAny<Guid>()), Times.Never);
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
