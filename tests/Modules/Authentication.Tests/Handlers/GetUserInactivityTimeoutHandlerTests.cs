using FluentAssertions;
using Moq;
using System.Security.Claims;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class GetUserInactivityTimeoutHandlerTests
{
    private sealed class TestRequestContext(IReadOnlyList<string> roles) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => "admin-1";
        public string? TenantId => "tenant-1";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "test"));
        public Vitarish.Requests.Domain.Entities.Request GetRequest() => new() { Authority = Authority, TenantId = TenantId, UserId = UserId };
    }

    [Fact]
    public async Task Administrator_reads_the_existing_user_override_and_version()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationUserDescriptor("user-1", "operator", null, null, null, null, null, false));
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.GetUserInactivityTimeoutStateAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (int?)12, 3L));
        GetUserInactivityTimeoutHandler handler = new(directory.Object, sessions.Object, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        var result = await handler.Handle("user-1", CancellationToken.None);

        result.Should().BeEquivalentTo(new { Version = 3L, InactivityTimeoutMinutes = (int?)12 });
        sessions.Verify(item => item.GetUserInactivityTimeoutStateAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Missing_user_returns_not_found_without_reading_policy_state()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((AuthenticationUserDescriptor?)null);
        Mock<IAuthenticationSessionRegistry> sessions = new();
        GetUserInactivityTimeoutHandler handler = new(directory.Object, sessions.Object, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        var result = await handler.Handle("missing", CancellationToken.None);

        result.Should().BeNull();
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Non_administrator_cannot_read_user_policy_state()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        Mock<IAuthenticationSessionRegistry> sessions = new();
        GetUserInactivityTimeoutHandler handler = new(directory.Object, sessions.Object, new TestRequestContext([]));

        Func<Task> act = () => handler.Handle("user-1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        directory.VerifyNoOtherCalls();
        sessions.VerifyNoOtherCalls();
    }
}
