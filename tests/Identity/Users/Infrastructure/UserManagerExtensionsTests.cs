using Users.Tests.TestHelpers;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Users.Tests.Infrastructure;

public class UserManagerExtensionsTests
{
    [Fact]
    public async Task CreateUserWithoutPasswordAsync_SetsNamesAndCreatesUser()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.SetUserNameAsync(It.IsAny<User>(), "user@example.com"))
            .Callback<User, string>((user, name) => user.UserName = name)
            .ReturnsAsync(IdentityResult.Success);
        _ = userManager.Setup(m => m.SetEmailAsync(It.IsAny<User>(), "user@example.com"))
            .Callback<User, string>((user, email) => user.Email = email)
            .ReturnsAsync(IdentityResult.Success);
        _ = userManager.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        User user = await userManager.Object.CreateUserWithoutPasswordAsync(
            "user@example.com",
            "First",
            "Last",
            u => userManager.Object.SetEmailAsync(u, "user@example.com"));

        _ = user.UserName.Should().Be("user@example.com");
        _ = user.Email.Should().Be("user@example.com");
        _ = user.FirstName.Should().Be("First");
        _ = user.LastName.Should().Be("Last");
        userManager.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserWithoutPasswordAsync_ThrowsWhenCreateFails()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _ = userManager.Setup(m => m.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _ = userManager.Setup(m => m.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "CreateFailed", Description = "fail" }));

        Func<Task> act = () => userManager.Object.CreateUserWithoutPasswordAsync(
            "user@example.com",
            "First",
            "Last",
            u => userManager.Object.SetEmailAsync(u, "user@example.com"));

        _ = await act.Should().ThrowAsync<Exception>()
            .WithMessage("*CreateFailed:fail*");
    }
}
