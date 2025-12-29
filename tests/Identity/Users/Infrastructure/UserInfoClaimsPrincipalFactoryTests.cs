using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Infrastructure;

public class UserInfoClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task CreateAsync_AddsGivenAndFamilyNameClaims()
    {
        User user = new() { Id = "user-1", UserName = "user1", FirstName = "First", LastName = "Last" };
        UserManager<User> userManager = CreateUserManager(user, new List<Claim>(), new List<string>());
        UserInfoClaimsPrincipalFactory factory = new(userManager, Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = await factory.CreateAsync(user);

        principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.GivenName && c.Value == "First");
        principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.FamilyName && c.Value == "Last");
    }

    [Fact]
    public async Task CreateAsync_UsesEmptyValuesWhenNamesNull()
    {
        User user = new() { Id = "user-2", UserName = "user2" };
        UserManager<User> userManager = CreateUserManager(user, new List<Claim>(), new List<string>());
        UserInfoClaimsPrincipalFactory factory = new(userManager, Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = await factory.CreateAsync(user);

        principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.GivenName && c.Value == string.Empty);
        principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.FamilyName && c.Value == string.Empty);
    }

    private static UserManager<User> CreateUserManager(User user, IList<Claim> claims, IList<string> roles)
    {
        Mock<IUserStore<User>> store = new();
        store.Setup(s => s.GetUserIdAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.Id);
        store.Setup(s => s.GetUserNameAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.UserName);

        Mock<IUserClaimStore<User>> claimStore = store.As<IUserClaimStore<User>>();
        claimStore.Setup(s => s.GetClaimsAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(claims);

        Mock<IUserRoleStore<User>> roleStore = store.As<IUserRoleStore<User>>();
        roleStore.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        return new UserManager<User>(
            store.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }
}
