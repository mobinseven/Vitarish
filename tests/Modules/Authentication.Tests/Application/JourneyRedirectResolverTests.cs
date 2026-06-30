using FluentAssertions;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure.ReturnUrls;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Xunit;

namespace Authentication.Tests.Application;

public class JourneyRedirectResolverTests
{
    private readonly Mock<IJourneyResolver> _journeyResolverMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<ILogger<JourneyAwareRedirectResolver>> _loggerMock = new();

    [Fact]
    public async Task ResolveAsync_AuthenticatedUser_DelegatesToJourney()
    {
        // Arrange
        var userId = "user-123";
        var returnUrl = "/dashboard";

        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var httpContext = new DefaultHttpContext { User = claims };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var journeyStep = new JourneyStep(
            JourneyDestinationKind.ReturnUrl,
            "/home");

        _journeyResolverMock.Setup(x => x.ResolveAsync(
            It.Is<JourneyRequest>(r => r.UserId == userId && r.RequestedReturnUrl == returnUrl),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(journeyStep);

        var resolver = new JourneyAwareRedirectResolver(
            _journeyResolverMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync(returnUrl, false);

        // Assert
        result.Should().Be("/home");
        _journeyResolverMock.Verify(x => x.ResolveAsync(
            It.Is<JourneyRequest>(r => r.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UnauthenticatedUser_FallsBackToLegacy()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var resolver = new JourneyAwareRedirectResolver(
            _journeyResolverMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("/settings", false);

        // Assert
        result.Should().Be("/settings");
        _journeyResolverMock.Verify(x => x.ResolveAsync(
            It.IsAny<JourneyRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_NullHttpContext_FallsBackToLegacy()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var resolver = new JourneyAwareRedirectResolver(
            _journeyResolverMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("/home", false);

        // Assert
        result.Should().Be("/home");
    }

    [Fact]
    public async Task ResolveAsync_JourneyThrows_FallsBackToLegacy()
    {
        // Arrange
        var userId = "user-123";
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var httpContext = new DefaultHttpContext { User = claims };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _journeyResolverMock.Setup(x => x.ResolveAsync(
            It.IsAny<JourneyRequest>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Journey resolution failed"));

        var resolver = new JourneyAwareRedirectResolver(
            _journeyResolverMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object);

        // Act
        var result = await resolver.ResolveAsync("/dashboard", false);

        // Assert
        result.Should().Be("/dashboard");
    }
}
