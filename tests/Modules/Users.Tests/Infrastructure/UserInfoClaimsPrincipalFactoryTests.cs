using Duende.IdentityModel;
using System.Security.Claims;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Service;

namespace Users.Tests.Infrastructure;

public class UserInfoClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task CreateAsync_AddsGivenAndFamilyNameClaims()
    {
        User user = new() { Id = "user-1", UserName = "user1", FirstName = "First", LastName = "Last" };
        UserManager<User> userManager = CreateUserManager(user, [], []);
        UserInfoClaimsPrincipalFactory factory = new(userManager, Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = await factory.CreateAsync(user);

        _ = principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.GivenName && c.Value == "First");
        _ = principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.FamilyName && c.Value == "Last");
    }

    [Fact]
    public async Task CreateAsync_UsesEmptyValuesWhenNamesNull()
    {
        User user = new() { Id = "user-2", UserName = "user2" };
        UserManager<User> userManager = CreateUserManager(user, [], []);
        UserInfoClaimsPrincipalFactory factory = new(userManager, Options.Create(new IdentityOptions()));

        ClaimsPrincipal principal = await factory.CreateAsync(user);

        _ = principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.GivenName && c.Value == string.Empty);
        _ = principal.Claims.Should().Contain(c => c.Type == JwtClaimTypes.FamilyName && c.Value == string.Empty);
    }

    private static UserManager<User> CreateUserManager(User user, IList<Claim> claims, IList<string> roles)
    {
        Mock<IUserStore<User>> store = new();
        _ = store.Setup(s => s.GetUserIdAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.Id);
        _ = store.Setup(s => s.GetUserNameAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(user.UserName);

        Mock<IUserClaimStore<User>> claimStore = store.As<IUserClaimStore<User>>();
        _ = claimStore.Setup(s => s.GetClaimsAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(claims);

        Mock<IUserRoleStore<User>> roleStore = store.As<IUserRoleStore<User>>();
        _ = roleStore.Setup(s => s.GetRolesAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(roles);

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
