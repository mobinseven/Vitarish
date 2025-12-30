using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class IdentityRevalidatingAuthenticationStateProviderTests
{
    [Fact]
    public async Task IdentityRevalidatingAuthenticationStateProvider_Revalidates_WhenUserChanged()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        User user = new() { Id = "user-1" };

        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManager.Setup(manager => manager.GetSecurityStampAsync(user))
            .ReturnsAsync("new-stamp");
        userManager.SetupGet(manager => manager.SupportsUserSecurityStamp).Returns(true);

        ServiceProvider serviceProvider = BuildServiceProvider(userManager.Object);
        using LoggerFactory loggerFactory = new();
        TestIdentityRevalidatingAuthenticationStateProvider sut = new(
            loggerFactory,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = BuildPrincipal("old-stamp");
        AuthenticationState state = new(principal);

        bool isValid = await sut.ValidateAsync(state);

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task IdentityRevalidatingAuthenticationStateProvider_DoesNotRevalidate_WhenUnchanged()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        User user = new() { Id = "user-1" };

        userManager.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManager.Setup(manager => manager.GetSecurityStampAsync(user))
            .ReturnsAsync("stamp");
        userManager.SetupGet(manager => manager.SupportsUserSecurityStamp).Returns(true);

        ServiceProvider serviceProvider = BuildServiceProvider(userManager.Object);
        using LoggerFactory loggerFactory = new();
        TestIdentityRevalidatingAuthenticationStateProvider sut = new(
            loggerFactory,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = BuildPrincipal("stamp");
        AuthenticationState state = new(principal);

        bool isValid = await sut.ValidateAsync(state);

        isValid.Should().BeTrue();
    }

    private static ClaimsPrincipal BuildPrincipal(string stamp)
    {
        ClaimsIdentity identity = new(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim("AspNet.Identity.SecurityStamp", stamp)
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static ServiceProvider BuildServiceProvider(UserManager<User> userManager)
    {
        ServiceCollection services = new();
        _ = services.AddScoped(_ => userManager);
        return services.BuildServiceProvider();
    }

    private sealed class TestIdentityRevalidatingAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> options)
        : IdentityRevalidatingAuthenticationStateProvider(loggerFactory, scopeFactory, options)
    {
        public Task<bool> ValidateAsync(AuthenticationState state, CancellationToken cancellationToken = default)
            => ValidateAuthenticationStateAsync(state, cancellationToken);
    }
}
