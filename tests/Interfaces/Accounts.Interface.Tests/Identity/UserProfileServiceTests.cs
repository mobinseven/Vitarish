using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Vitarish.Accounts.Infrastructure.Services;
using ApplicationUserProfileService = Vitarish.Users.Application.Abstractions.IUserProfileService;

namespace Accounts.Interface.Tests.Identity;

public class UserProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsPreferences_WhenUserAuthenticated()
    {
        // Arrange
        Dictionary<string, string> preferences = new() { ["Culture"] = "en-US" };
        Mock<ApplicationUserProfileService> profileService = new();
        _ = profileService
            .Setup(s => s.GetProfileAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        AuthenticationStateProvider authProvider = new StaticAuthenticationStateProvider(CreateUser("user1"));
        UserProfileInterfaceService service = new(authProvider, profileService.Object);

        // Act
        Dictionary<string, string>? result = await service.GetProfileAsync();

        // Assert
        _ = result.Should().BeEquivalentTo(preferences);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenUserNotAuthenticated()
    {
        // Arrange
        Mock<ApplicationUserProfileService> profileService = new(MockBehavior.Strict);
        AuthenticationStateProvider authProvider = new StaticAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity()));
        UserProfileInterfaceService service = new(authProvider, profileService.Object);

        // Act
        Dictionary<string, string>? result = await service.GetProfileAsync();

        // Assert
        _ = result.Should().BeNull();
        profileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProfileAsync_NoOps_WhenUserIdMissing()
    {
        // Arrange
        Mock<ApplicationUserProfileService> profileService = new(MockBehavior.Strict);
        ClaimsIdentity identity = new(new[] { new Claim("role", "user") }, "Test");
        AuthenticationStateProvider authProvider = new StaticAuthenticationStateProvider(new ClaimsPrincipal(identity));
        UserProfileInterfaceService service = new(authProvider, profileService.Object);

        // Act
        await service.UpdateProfileAsync([]);

        // Assert
        profileService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetCultureAsync_UpdatesProfile_WhenCultureValid()
    {
        // Arrange
        Dictionary<string, string> preferences = [];
        Mock<ApplicationUserProfileService> profileService = new();
        _ = profileService
            .Setup(s => s.GetProfileAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _ = profileService
            .Setup(s => s.UpdateProfileAsync("user1", preferences, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AuthenticationStateProvider authProvider = new StaticAuthenticationStateProvider(CreateUser("user1"));
        UserProfileInterfaceService service = new(authProvider, profileService.Object);

        // Act
        bool result = await service.SetCultureAsync("nb-NO");

        // Assert
        _ = result.Should().BeTrue();
        _ = preferences["Culture"].Should().Be("nb-NO");
        profileService.Verify(s => s.UpdateProfileAsync("user1", preferences, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCultureAsync_ReturnsFalse_WhenCultureBlank()
    {
        // Arrange
        Mock<ApplicationUserProfileService> profileService = new();
        AuthenticationStateProvider authProvider = new StaticAuthenticationStateProvider(CreateUser("user1"));
        UserProfileInterfaceService service = new(authProvider, profileService.Object);

        // Act
        bool result = await service.SetCultureAsync("  ");

        // Assert
        _ = result.Should().BeFalse();
        profileService.VerifyNoOtherCalls();
    }

    private static ClaimsPrincipal CreateUser(string userId)
    {
        ClaimsIdentity identity = new("Test");
        identity.AddClaim(new Claim("sub", userId));
        return new ClaimsPrincipal(identity);
    }

    private sealed class StaticAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        private readonly AuthenticationState state = new(principal);

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
    }
}
