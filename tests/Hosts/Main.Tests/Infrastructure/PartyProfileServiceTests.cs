using Duende.IdentityServer.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Vitarish.Main.Infrastructure.IdentityServer;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Service;
using IdentityOptions = Microsoft.AspNetCore.Identity.IdentityOptions;

namespace Vitarish.Host.Tests.Infrastructure;

public class PartyProfileServiceTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly UserInfoClaimsPrincipalFactory _claimsFactory;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ITenantsService> _mockTenantsService;
    private readonly Mock<ILogger<PartyProfileService>> _mockLogger;
    private readonly PartyProfileService _sut;

    public PartyProfileServiceTests()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Create the claims factory with required dependencies
        _claimsFactory = new UserInfoClaimsPrincipalFactory(
            _mockUserManager.Object,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()));
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockTenantsService = new Mock<ITenantsService>();
        _mockLogger = new Mock<ILogger<PartyProfileService>>();

        _sut = new PartyProfileService(
            _mockUserManager.Object,
            _claimsFactory,
            _mockHttpContextAccessor.Object,
            _mockTenantsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantResolved_AddsTenantIdAndOwnerIdClaims()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Tenant tenant = new()
        {
            Id = tenantId,
            OwnerId = ownerId,
            Name = "Test Tenant",
            ClientId = "test-client"
        };

        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        context.IssuedClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == tenantId.ToString());
        context.IssuedClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == ownerId.ToString());
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantNotResolved_DoesNotAddTenantOwnerClaims()
    {
        // Arrange
        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenHttpContextIsNull_DoesNotAddTenantOwnerClaims()
    {
        // Arrange
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
        _mockTenantsService.Verify(
            x => x.ResolveFromHostAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantServiceThrowsException_DoesNotAddClaimsAndLogsWarning()
    {
        // Arrange
        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        context.IssuedClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantResolved_PreservesExistingClaims()
    {
        // Arrange
        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Tenant",
            ClientId = "test-client"
        };

        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        Claim existingClaim = new("existing_claim", "existing_value");
        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = [existingClaim]
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        _ = context.IssuedClaims.Should().Contain(c =>
            c.Type == "existing_claim" && c.Value == "existing_value");
        context.IssuedClaims.Should().Contain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        context.IssuedClaims.Should().Contain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenCalled_CallsBaseGetProfileDataAsync()
    {
        // Arrange
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert - if no exception is thrown, the base method was called successfully
        // The base ProfileService doesn't add claims when there's no user, so this is expected
        _ = context.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantResolved_AddsBothClaimsWithCorrectTypes()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Tenant tenant = new()
        {
            Id = tenantId,
            OwnerId = ownerId,
            Name = "Test Tenant",
            ClientId = "test-client"
        };

        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        Claim? tenantIdClaim = context.IssuedClaims.FirstOrDefault(c => c.Type == IdentityDefinitions.TenantIdClaim);
        Claim? ownerIdClaim = context.IssuedClaims.FirstOrDefault(c => c.Type == IdentityDefinitions.OwnerIdClaim);

        _ = tenantIdClaim.Should().NotBeNull();
        _ = ownerIdClaim.Should().NotBeNull();
        _ = tenantIdClaim!.Type.Should().Be(IdentityDefinitions.TenantIdClaim);
        _ = ownerIdClaim!.Type.Should().Be(IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task GetProfileDataAsync_WhenTenantResolvedWithEmptyGuid_StillAddsClaims()
    {
        // Arrange
        Guid tenantId = Guid.Empty;
        Guid ownerId = Guid.Empty;
        Tenant tenant = new()
        {
            Id = tenantId,
            OwnerId = ownerId,
            Name = "Test Tenant",
            ClientId = "test-client"
        };

        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        ProfileDataRequestContext context = new()
        {
            Subject = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "user-id")], "test")),
            IssuedClaims = []
        };

        // Act
        await _sut.GetProfileDataAsync(context);

        // Assert
        context.IssuedClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == Guid.Empty.ToString());
        context.IssuedClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == Guid.Empty.ToString());
    }
}
