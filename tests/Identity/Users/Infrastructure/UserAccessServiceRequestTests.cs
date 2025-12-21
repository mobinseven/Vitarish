using MediatR;
using Vitarish.Requests.Domain.Models;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Infrastructure;

public class UserAccessServiceRequestTests
{
    private readonly Mock<ISender> sender = new();
    private readonly UserAccessService service;

    public UserAccessServiceRequestTests()
    {
        service = new UserAccessService(sender.Object);
    }

    [Fact]
    public async Task HasRequestAccessAsync_SendsRequestBasedQuery()
    {
        // Arrange
        RequestIdentityDto request = new(
            RequestId: "req-123",
            Authority: "https://auth.example.com",
            ClientId: "client-1",
            OwnerId: "owner-1",
            TenantId: "tenant-1",
            UserId: "user-123",
            ResourceId: "/api/resource",
            TraceId: "trace-123",
            AdditionalInfo: null
        );

        sender.Setup(s => s.Send(It.IsAny<HasRequestAccessQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        // Act
        bool result = await service.HasRequestAccessAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        sender.Verify(s => s.Send(
            It.Is<HasRequestAccessQuery>(q => q.Request == request),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRequestAccessClaimAsync_SendsRequestBasedQuery()
    {
        // Arrange
        RequestIdentityDto request = new(
            RequestId: "req-456",
            Authority: "https://auth.example.com",
            ClientId: "client-2",
            OwnerId: "owner-2",
            TenantId: null,
            UserId: "user-456",
            ResourceId: "/api/admin",
            TraceId: null,
            AdditionalInfo: null
        );

        ClaimDto expectedClaim = new("access", "client-2:admin");

        sender.Setup(s => s.Send(It.IsAny<GetRequestAccessClaimQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<ClaimDto?>(expectedClaim));

        // Act
        ClaimDto? result = await service.GetRequestAccessClaimAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedClaim);
        sender.Verify(s => s.Send(
            It.Is<GetRequestAccessClaimQuery>(q => q.Request == request),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetRequestAccessAsync_SendsRequestBasedCommand()
    {
        // Arrange
        RequestIdentityDto request = new(
            RequestId: "req-789",
            Authority: "https://auth.example.com",
            ClientId: "client-3",
            OwnerId: "owner-3",
            TenantId: "tenant-3",
            UserId: "user-789",
            ResourceId: null,
            TraceId: null,
            AdditionalInfo: null
        );

        SetRequestAccessClaimCommand command = new(request, "reader");

        sender.Setup(s => s.Send(It.IsAny<SetRequestAccessClaimCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await service.SetRequestAccessAsync(command, CancellationToken.None);

        // Assert
        sender.Verify(s => s.Send(
            It.Is<SetRequestAccessClaimCommand>(cmd => cmd.Request == request && cmd.AccessValue == "reader"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveRequestAccessAsync_SendsRequestBasedCommand()
    {
        // Arrange
        RequestIdentityDto request = new(
            RequestId: "req-101",
            Authority: "https://auth.example.com",
            ClientId: "client-4",
            OwnerId: "owner-4",
            TenantId: null,
            UserId: "user-101",
            ResourceId: null,
            TraceId: null,
            AdditionalInfo: null
        );

        RemoveRequestAccessClaimCommand command = new(request);

        sender.Setup(s => s.Send(It.IsAny<RemoveRequestAccessClaimCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        // Act
        await service.RemoveRequestAccessAsync(command, CancellationToken.None);

        // Assert
        sender.Verify(s => s.Send(
            It.Is<RemoveRequestAccessClaimCommand>(cmd => cmd.Request == request),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HasUserAccessAsync_StillWorksForBackwardCompatibility()
    {
        // Arrange
        sender.Setup(s => s.Send(It.IsAny<HasUserAccessQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(false));

        // Act
#pragma warning disable CS0618 // Type or member is obsolete
        bool result = await service.HasUserAccessAsync("user-999", "client-5", CancellationToken.None);
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert
        result.Should().BeFalse();
        sender.Verify(s => s.Send(
            It.Is<HasUserAccessQuery>(q => q.UserId == "user-999" && q.ClientId == "client-5"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
