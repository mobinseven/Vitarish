using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;

namespace Users.Tests.TestHelpers;

internal static class UserManagerMockFactory
{
    internal static Mock<UserManager<User>> Create()
    {
        Mock<IUserStore<User>> store = new();
        Mock<IOptions<IdentityOptions>> options = new();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        return new Mock<UserManager<User>>(
            store.Object,
            options.Object,
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }
}
