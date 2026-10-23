using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class SetUserProfileValueCommandHandlerTests
{
    [Fact]
    public async Task Sets_one_entry_and_keeps_every_other_entry()
    {
        User user = new() { Id = "user-1", ProfilePreferences = new() { ["Culture"] = "fa", ["other.entry"] = "kept" } };
        Mock<UserManager<User>> users = UserManager(user, IdentityResult.Success);

        SetUserProfileValueOutcome outcome = await Handler(users).Handle(new("user-1", "parties.onboarding.setup-draft", "{}"), default);

        outcome.Should().Be(SetUserProfileValueOutcome.Saved);
        user.ProfilePreferences.Should().Equal(new Dictionary<string, string>
        {
            ["Culture"] = "fa",
            ["other.entry"] = "kept",
            ["parties.onboarding.setup-draft"] = "{}",
        });
    }

    [Fact]
    public async Task A_null_value_removes_only_that_entry()
    {
        User user = new() { Id = "user-1", ProfilePreferences = new() { ["Culture"] = "en", ["parties.onboarding.setup-draft"] = "{}" } };
        Mock<UserManager<User>> users = UserManager(user, IdentityResult.Success);

        _ = await Handler(users).Handle(new("user-1", "parties.onboarding.setup-draft", null), default);

        user.ProfilePreferences.Should().Equal(new Dictionary<string, string> { ["Culture"] = "en" });
    }

    [Theory]
    [InlineData("Culture")]
    [InlineData("no-namespace")]
    [InlineData("Parties.Draft")]
    public async Task Refuses_keys_that_could_overwrite_the_profiles_own_settings(string key)
    {
        User user = new() { Id = "user-1", ProfilePreferences = new() { ["Culture"] = "en" } };
        Mock<UserManager<User>> users = UserManager(user, IdentityResult.Success);

        SetUserProfileValueOutcome outcome = await Handler(users).Handle(new("user-1", key, "x"), default);

        outcome.Should().Be(SetUserProfileValueOutcome.Invalid);
        users.Verify(manager => manager.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Reports_a_conflict_when_another_change_landed_first()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> users = UserManager(user, IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));

        SetUserProfileValueOutcome outcome = await Handler(users).Handle(new("user-1", "parties.onboarding.setup-draft", "{}"), default);

        outcome.Should().Be(SetUserProfileValueOutcome.Conflict);
    }

    private static SetUserProfileValueCommandHandler Handler(Mock<UserManager<User>> users) =>
        new(users.Object, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    private static Mock<UserManager<User>> UserManager(User user, IdentityResult updateResult)
    {
        Mock<UserManager<User>> manager = new(
            Mock.Of<IUserStore<User>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
        manager.Setup(value => value.FindByIdAsync(user.Id)).ReturnsAsync(user);
        manager.Setup(value => value.UpdateAsync(user)).ReturnsAsync(updateResult);
        return manager;
    }
}
