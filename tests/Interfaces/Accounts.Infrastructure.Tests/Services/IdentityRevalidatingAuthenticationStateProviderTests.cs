using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
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
        IdentityRevalidatingAuthenticationStateProvider sut = new(
            new LoggerFactory(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = BuildPrincipal("old-stamp");
        AuthenticationState state = new(principal);

        bool isValid = await ValidateAsync(sut, state);

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
        IdentityRevalidatingAuthenticationStateProvider sut = new(
            new LoggerFactory(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = BuildPrincipal("stamp");
        AuthenticationState state = new(principal);

        bool isValid = await ValidateAsync(sut, state);

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

    private static async Task<bool> ValidateAsync(
        IdentityRevalidatingAuthenticationStateProvider provider,
        AuthenticationState state)
    {
        MethodInfo? method = typeof(IdentityRevalidatingAuthenticationStateProvider)
            .GetMethod("ValidateAuthenticationStateAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull();
        Task<bool> task = (Task<bool>)method!.Invoke(provider, new object[] { state, CancellationToken.None })!;
        return await task;
    }
}
