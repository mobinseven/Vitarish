using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Vitarish.Users.Domain.Entities;

namespace Users.Tests.TestHelpers;

internal static class UserManagerFactory
{
    internal static UserManager<User> Create(UsersDbContext dbContext)
    {
        UserStore<User> store = new(dbContext);
        return new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            new Logger<UserManager<User>>(new LoggerFactory()));
    }
}
