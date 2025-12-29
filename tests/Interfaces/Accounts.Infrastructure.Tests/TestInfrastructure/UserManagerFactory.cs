using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;

public static class UserManagerFactory
{
    public static Mock<UserManager<User>> CreateMock()
    {
        Mock<IUserStore<User>> userStore = new();
        Mock<IOptions<IdentityOptions>> options = new();
        Mock<IPasswordHasher<User>> passwordHasher = new();
        List<IUserValidator<User>> userValidators = [];
        List<IPasswordValidator<User>> passwordValidators = [];
        Mock<ILookupNormalizer> keyNormalizer = new();
        IdentityErrorDescriber errors = new();
        Mock<IServiceProvider> services = new();
        Mock<ILogger<UserManager<User>>> logger = new();

        return new Mock<UserManager<User>>(
            userStore.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors,
            services.Object,
            logger.Object);
    }
}
