using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Host.Infrastructure.IdentityServer;
using Vitarish.Host.Services;
using IdentityClient = Duende.IdentityServer.Models.Client;

namespace Vitarish.Host.Tests.Infrastructure;

public class IdentityClientsStoreTests : IDisposable
{
    private readonly AuthenticationClientDbContext _authClientDbContext;
    private readonly ApiClientDbContext _apiClientDbContext;
    private readonly IdentityClientsStore _sut;

    public IdentityClientsStoreTests()
    {
        DbContextOptions<AuthenticationClientDbContext> authOptions = new DbContextOptionsBuilder<AuthenticationClientDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _authClientDbContext = new AuthenticationClientDbContext(authOptions);

        DbContextOptions<ApiClientDbContext> apiOptions = new DbContextOptionsBuilder<ApiClientDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _apiClientDbContext = new ApiClientDbContext(apiOptions);

        _sut = new IdentityClientsStore(_authClientDbContext, _apiClientDbContext);
    }

    public void Dispose()
    {
        _authClientDbContext.Dispose();
        _apiClientDbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasTenantId_ReturnsClientWithTenantIdClaim()
    {
        // Arrange
        string clientId = "test-api-client";
        string tenantId = "tenant-123";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = tenantId
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        result!.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == tenantId);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasOwnerId_ReturnsClientWithOwnerIdClaim()
    {
        // Arrange
        string clientId = "test-api-client";
        string ownerId = "owner-456";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            OwnerId = ownerId
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        result!.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == ownerId);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasBothTenantIdAndOwnerId_ReturnsClientWithBothClaims()
    {
        // Arrange
        string clientId = "test-api-client";
        string tenantId = "tenant-123";
        string ownerId = "owner-456";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = tenantId,
            OwnerId = ownerId
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Claims.Should().HaveCount(2);
        result.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == tenantId);
        result.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == ownerId);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasNoTenantIdOrOwnerId_ReturnsClientWithNoClaims()
    {
        // Arrange
        string clientId = "test-api-client";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = null,
            OwnerId = null
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasEmptyTenantIdAndOwnerId_ReturnsClientWithNoClaims()
    {
        // Arrange
        string clientId = "test-api-client";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = "",
            OwnerId = ""
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasClaims_ClientClaimsPrefixIsEmptyString()
    {
        // Arrange
        string clientId = "test-api-client";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = "tenant-123"
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ClientClaimsPrefix.Should().BeEmpty();
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasNoClaims_ClientClaimsPrefixIsEmptyString()
    {
        // Arrange
        string clientId = "test-api-client";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com"
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ClientClaimsPrefix.Should().BeEmpty();
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenAuthenticationClientExists_ReturnsAuthenticationClientWithoutTenantOwnerClaims()
    {
        // Arrange
        string clientId = "test-auth-client";
        AuthenticationClient authClient = new()
        {
            Id = clientId,
            Url = "example.com",
            Title = "Test Auth Client"
        };
        _ = _authClientDbContext.AuthenticationClients.Add(authClient);
        _ = await _authClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ClientId.Should().Be(clientId);
        result.Claims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
        result.Claims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenClientNotFound_ReturnsNull()
    {
        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync("non-existent-client");

        // Assert
        _ = result.Should().BeNull();
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClient_UsesClientCredentialsGrantType()
    {
        // Arrange
        string clientId = "test-api-client";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com"
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.AllowedGrantTypes.Should().BeEquivalentTo(Duende.IdentityServer.Models.GrantTypes.ClientCredentials);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasOnlyTenantId_ReturnsClientWithOnlyTenantIdClaim()
    {
        // Arrange
        string clientId = "test-api-client";
        string tenantId = "tenant-only";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = tenantId,
            OwnerId = null
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Claims.Should().HaveCount(1);
        result.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.TenantIdClaim && c.Value == tenantId);
        result.Claims.Should().NotContain(c => c.Type == IdentityDefinitions.OwnerIdClaim);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClientHasOnlyOwnerId_ReturnsClientWithOnlyOwnerIdClaim()
    {
        // Arrange
        string clientId = "test-api-client";
        string ownerId = "owner-only";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = "api",
            HostUri = "https://test.example.com",
            TenantId = null,
            OwnerId = ownerId
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Claims.Should().HaveCount(1);
        result.Claims.Should().ContainSingle(c =>
            c.Type == IdentityDefinitions.OwnerIdClaim && c.Value == ownerId);
        result.Claims.Should().NotContain(c => c.Type == IdentityDefinitions.TenantIdClaim);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClient_ConfiguresSecretCorrectly()
    {
        // Arrange
        string clientId = "test-api-client";
        string secret = "my-test-secret";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = secret,
            Scopes = "api",
            HostUri = "https://test.example.com"
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.ClientSecrets.Should().ContainSingle();
        // The secret should be hashed (SHA256)
        _ = result.ClientSecrets.First().Value.Should().NotBeNullOrEmpty();
        _ = result.ClientSecrets.First().Value.Should().NotBe(secret);
    }

    [Fact]
    public async Task FindClientByIdAsync_WhenApiClient_ConfiguresAllowedScopesCorrectly()
    {
        // Arrange
        string clientId = "test-api-client";
        string scopes = "api.read";
        ApiClient apiClient = new()
        {
            Id = clientId,
            Secret = "test-secret",
            Scopes = scopes,
            HostUri = "https://test.example.com"
        };
        _ = _apiClientDbContext.ApiClients.Add(apiClient);
        _ = await _apiClientDbContext.SaveChangesAsync();

        // Act
        IdentityClient? result = await _sut.FindClientByIdAsync(clientId);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.AllowedScopes.Should().Contain(scopes);
    }
}
