
using global::Vitarish.Main.Infrastructure.IdentityServer;
using global::Vitarish.Main.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace Main.Tests.Services;

public sealed class TenantRoleClaimsTransformationTests
{
    private static ClaimsPrincipal CreatePrincipalWithUserId(string userId, string authType = "Cookies")
    {
        ClaimsIdentity identity = new(authenticationType: authType);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task TransformAsync_returns_same_principal_when_HttpContext_is_null()
    {
        // Arrange
        HttpContextAccessor accessor = new()
        { HttpContext = null };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        ClaimsPrincipal principal = CreatePrincipalWithUserId("user-1");

        // Act
        ClaimsPrincipal result = await sut.TransformAsync(principal);

        // Assert
        Assert.Same(principal, result);
        tenantResolver.VerifyNoOtherCalls();
        roleStore.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TransformAsync_returns_same_principal_when_tenantId_is_missing(string? tenantId)
    {
        // Arrange
        DefaultHttpContext ctx = new();
        HttpContextAccessor accessor = new()
        { HttpContext = ctx };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        _ = tenantResolver.Setup(r => r.ResolveTenantId(ctx)).Returns(tenantId);

        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        ClaimsPrincipal principal = CreatePrincipalWithUserId("user-1");

        // Act
        ClaimsPrincipal result = await sut.TransformAsync(principal);

        // Assert
        Assert.Same(principal, result);
        tenantResolver.Verify(r => r.ResolveTenantId(ctx), Times.Once);
        roleStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TransformAsync_returns_same_principal_when_userId_is_missing()
    {
        // Arrange
        DefaultHttpContext ctx = new();
        HttpContextAccessor accessor = new()
        { HttpContext = ctx };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        _ = tenantResolver.Setup(r => r.ResolveTenantId(ctx)).Returns("tenant-1");

        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        // principal WITHOUT NameIdentifier
        ClaimsPrincipal principal = new(new ClaimsIdentity(authenticationType: "Cookies"));

        // Act
        ClaimsPrincipal result = await sut.TransformAsync(principal);

        // Assert
        Assert.Same(principal, result);
        tenantResolver.Verify(r => r.ResolveTenantId(ctx), Times.Once);
        roleStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TransformAsync_adds_tenant_identity_with_tenantId_claim_and_role_claims_and_preserves_existing_identities()
    {
        // Arrange
        DefaultHttpContext ctx = new();
        HttpContextAccessor accessor = new()
        { HttpContext = ctx };

        const string tenantId = "tenant-1";
        const string userId = "user-1";
        string[] roles = new[] { "Admin", "Editor" };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        _ = tenantResolver.Setup(r => r.ResolveTenantId(ctx)).Returns(tenantId);

        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);
        _ = roleStore.Setup(s => s.GetRolesAsync(userId, tenantId)).ReturnsAsync(roles);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        ClaimsIdentity baseIdentity = new(authenticationType: "Cookies");
        baseIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        baseIdentity.AddClaim(new Claim("some", "claim"));

        ClaimsPrincipal principal = new(baseIdentity);

        // Act
        ClaimsPrincipal result = await sut.TransformAsync(principal);

        // Assert: new principal instance (implementation returns a rebuilt principal)
        Assert.NotSame(principal, result);

        // Original identity preserved
        Assert.Contains(result.Identities, i => i.AuthenticationType == "Cookies");

        // Tenant identity added
        ClaimsIdentity tenantIdentity = Assert.Single(
            result.Identities.Where(i => i.AuthenticationType == IdentityDefinitions.TenantRolesAuthType));

        Assert.Equal(
            tenantId,
            tenantIdentity.FindFirst(IdentityDefinitions.TenantIdClaim)?.Value);

        string[] roleValues = tenantIdentity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        Assert.Equal(roles, roleValues);

        // Proves it will satisfy [Authorize(Roles="Admin")] style checks
        Assert.True(result.IsInRole("Admin"));
        Assert.True(result.IsInRole("Editor"));
        Assert.False(result.IsInRole("Nope"));

        tenantResolver.Verify(r => r.ResolveTenantId(ctx), Times.Once);
        roleStore.Verify(s => s.GetRolesAsync(userId, tenantId), Times.Once);
        roleStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TransformAsync_replaces_existing_tenant_roles_identity_instead_of_accumulating()
    {
        // Arrange
        DefaultHttpContext ctx = new();
        HttpContextAccessor accessor = new()
        { HttpContext = ctx };

        const string tenantId = "tenant-new";
        const string userId = "user-1";
        string[] roles = new[] { "Editor" };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        _ = tenantResolver.Setup(r => r.ResolveTenantId(ctx)).Returns(tenantId);

        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);
        _ = roleStore.Setup(s => s.GetRolesAsync(userId, tenantId)).ReturnsAsync(roles);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        ClaimsIdentity baseIdentity = new(authenticationType: "Cookies");
        baseIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

        // Existing tenant-role identity that should be removed
        ClaimsIdentity oldTenantIdentity = new(IdentityDefinitions.TenantRolesAuthType);
        oldTenantIdentity.AddClaim(new Claim(IdentityDefinitions.TenantIdClaim, "tenant-old"));
        oldTenantIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));

        ClaimsPrincipal principal = new(new[] { baseIdentity, oldTenantIdentity });

        // Act
        ClaimsPrincipal result = await sut.TransformAsync(principal);

        // Assert: only ONE tenant-role identity remains
        List<ClaimsIdentity> tenantIdentities = result.Identities
            .Where(i => i.AuthenticationType == IdentityDefinitions.TenantRolesAuthType)
            .ToList();
        _ = Assert.Single(tenantIdentities);

        ClaimsIdentity tenantIdentity = tenantIdentities[0];
        Assert.Equal(tenantId, tenantIdentity.FindFirst(IdentityDefinitions.TenantIdClaim)?.Value);

        Assert.False(result.IsInRole("Admin"));  // old role removed
        Assert.True(result.IsInRole("Editor"));  // new role added
    }

    [Fact]
    public async Task TransformAsync_called_twice_does_not_duplicate_tenant_identity_or_role_claims()
    {
        // Arrange
        DefaultHttpContext ctx = new();
        HttpContextAccessor accessor = new()
        { HttpContext = ctx };

        const string tenantId = "tenant-1";
        const string userId = "user-1";
        string[] roles = new[] { "Admin", "Editor" };

        Mock<ITenantResolver> tenantResolver = new(MockBehavior.Strict);
        _ = tenantResolver.Setup(r => r.ResolveTenantId(ctx)).Returns(tenantId);

        Mock<ITenantRoleStore> roleStore = new(MockBehavior.Strict);
        _ = roleStore.Setup(s => s.GetRolesAsync(userId, tenantId)).ReturnsAsync(roles);

        TenantRoleClaimsTransformation sut = new(accessor, tenantResolver.Object, roleStore.Object);

        ClaimsPrincipal principal = CreatePrincipalWithUserId(userId);

        // Act
        ClaimsPrincipal once = await sut.TransformAsync(principal);
        ClaimsPrincipal twice = await sut.TransformAsync(once);

        // Assert
        List<ClaimsIdentity> tenantIdentities = twice.Identities
            .Where(i => i.AuthenticationType == IdentityDefinitions.TenantRolesAuthType)
            .ToList();
        _ = Assert.Single(tenantIdentities);

        ClaimsIdentity tenantIdentity = tenantIdentities[0];

        string[] roleValues = tenantIdentity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        Assert.Equal(roles, roleValues);

        Assert.True(twice.IsInRole("Admin"));
        Assert.True(twice.IsInRole("Editor"));

        // Called twice because we called TransformAsync twice (no caching in this implementation)
        roleStore.Verify(s => s.GetRolesAsync(userId, tenantId), Times.Exactly(2));
    }
}
