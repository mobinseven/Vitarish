using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Accounts.Interface.Components.Account.Widgets;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Users.Domain.Entities;
using System.Security.Claims;

namespace Accounts.Interface.Tests.Account;

public class TenantsWidgetTests
{
    [Fact]
    public async Task TenantsWidget_RendersTenantList()
    {
        await using InterfaceTestContext context = new();
        Guid tenantId = Guid.NewGuid();
        Owner owner = new() { Id = Guid.NewGuid(), Name = "Owner" };
        Tenant tenant = new() { Id = tenantId, Name = "Workspace A", OwnerId = owner.Id, ClientId = "client-1" };
        ApiClient apiClient = new()
        {
            Id = "client-1",
            Secret = "secret",
            Scopes = "scope.read",
            HostUri = "https://workspace.example.com"
        };
        TenantIndicator indicator = new() { TenantId = tenantId, Value = "workspace.example.com", IsPrimary = true };
        User user = new() { Id = "user-1", UserName = "alice" };

        _ = context.Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticatedStateProvider("user-1"));

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.GetUserRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityRole> { new($"{tenantId}:Admin") });
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantIndicatorsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantIndicator> { indicator });
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(apiClient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiClient);

        IRenderedComponent<TenantsWidget> cut = context.Render<TenantsWidget>();

        cut.WaitForAssertion(() => Assert.Contains("Workspace A", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task TenantsWidget_RendersRolesPerTenant()
    {
        await using InterfaceTestContext context = new();
        Guid tenantId = Guid.NewGuid();
        Owner owner = new() { Id = Guid.NewGuid(), Name = "Owner" };
        Tenant tenant = new() { Id = tenantId, Name = "Workspace A", OwnerId = owner.Id, ClientId = "client-1" };
        User user = new() { Id = "user-1", UserName = "alice" };

        _ = context.Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticatedStateProvider("user-1"));

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.GetUserRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityRole> { new($"{tenantId}:Viewer") });
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantIndicatorsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TenantIndicator>());

        IRenderedComponent<TenantsWidget> cut = context.Render<TenantsWidget>();

        cut.WaitForAssertion(() => Assert.Contains("Viewer", cut.Markup, StringComparison.Ordinal));
    }

    private sealed class TestAuthenticatedStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState state;

        public TestAuthenticatedStateProvider(string userId)
        {
            ClaimsPrincipal principal = new(new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "Cookies"));
            state = new AuthenticationState(principal);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
    }
}
