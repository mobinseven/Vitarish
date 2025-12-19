using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Vitarish.Main.Infrastructure.IdentityServer;
using Vitarish.Main.Services;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Host.Tests.Infrastructure;

public class ApiClientClaimsServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ITenantsService> _mockTenantsService;
    private readonly Mock<ILogger<ApiClientClaimsService>> _mockLogger;
    private readonly ApiClientClaimsService _sut;

    public ApiClientClaimsServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockTenantsService = new Mock<ITenantsService>();
        _mockLogger = new Mock<ILogger<ApiClientClaimsService>>();

        _sut = new ApiClientClaimsService(
            _mockHttpContextAccessor.Object,
            _mockTenantsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ValidateAsync_WhenClientCredentialsAndTenantResolved_AddsTenantOwnerClaims()
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

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _ = context.Result.ValidatedRequest.ClientClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == tenantId.ToString());
        _ = context.Result.ValidatedRequest.ClientClaims.Should().Contain(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == ownerId.ToString());
    }

    [Fact]
    public async Task ValidateAsync_WhenTenantNotResolved_DoesNotAddClaims()
    {
        // Arrange
        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task ValidateAsync_WhenHttpContextIsNull_DoesNotAddClaims()
    {
        // Arrange
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
        _mockTenantsService.Verify(
            x => x.ResolveFromHostAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WhenTenantServiceThrows_DoesNotAddClaimsAndLogsWarning()
    {
        // Arrange
        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _ = _mockTenantsService
            .Setup(x => x.ResolveFromHostAsync(httpContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        _ = context.Result.ValidatedRequest.ClientClaims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Theory]
    [InlineData(GrantType.AuthorizationCode)]
    [InlineData(GrantType.RefreshToken)]
    [InlineData(GrantType.Implicit)]
    public async Task ValidateAsync_WhenNotClientCredentialsGrant_DoesNotProcessClaims(string grantType)
    {
        // Arrange
        HttpContext httpContext = new DefaultHttpContext();
        _ = _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = grantType,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _mockTenantsService.Verify(
            x => x.ResolveFromHostAsync(It.IsAny<HttpContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_WhenTenantResolved_PreservesExistingClaims()
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
        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = [existingClaim]
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        _ = context.Result.ValidatedRequest.ClientClaims.Should().Contain(c =>
            c.Type == "existing_claim" && c.Value == "existing_value");
        _ = context.Result.ValidatedRequest.ClientClaims.Should().Contain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        _ = context.Result.ValidatedRequest.ClientClaims.Should().Contain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task ValidateAsync_WhenTenantResolved_AddsBothClaimsWithCorrectTypes()
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

        ValidatedTokenRequest validatedRequest = new()
        {
            GrantType = GrantType.ClientCredentials,
            Client = new Client { ClientId = "test-api-client" },
            ClientClaims = []
        };

        CustomTokenRequestValidationContext context = new()
        {
            Result = new TokenRequestValidationResult(validatedRequest)
        };

        // Act
        await _sut.ValidateAsync(context);

        // Assert
        Claim? tenantIdClaim = context.Result.ValidatedRequest.ClientClaims.FirstOrDefault(c => c.Type == IdentityDefinitions.TenantIdClaim);
        Claim? ownerIdClaim = context.Result.ValidatedRequest.ClientClaims.FirstOrDefault(c => c.Type == IdentityDefinitions.OwnerIdClaim);

        _ = tenantIdClaim.Should().NotBeNull();
        _ = ownerIdClaim.Should().NotBeNull();
        _ = tenantIdClaim!.Type.Should().Be("tenant_id");
        _ = ownerIdClaim!.Type.Should().Be("owner_id");
    }
}
