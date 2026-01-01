using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Vitarish.Main.Authorization;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Host.Tests.Authorization;

public class AuthorizerTests
{
    [Fact]
    public async Task HandleRequirementAsync_CachesRoleResult()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        Mock<UserManager<User>> userManager = new(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        User user = new();
        const string userId = "user-1";
        const string role = "Admin";

        _ = userManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _ = userManager.Setup(m => m.IsInRoleAsync(user, role)).ReturnsAsync(true);

        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));

        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", userId)], "mock"));
        Authorizer handler = new(userManager.Object, cache);
        AuthorizerRequirement requirement = new(role);

        AuthorizationHandlerContext firstContext = new([requirement], principal, null);
        await handler.HandleAsync(firstContext);

        AuthorizationHandlerContext secondContext = new([requirement], principal, null);
        await handler.HandleAsync(secondContext);

        firstContext.HasSucceeded.Should().BeTrue();
        secondContext.HasSucceeded.Should().BeTrue();
        userManager.Verify(m => m.FindByIdAsync(userId), Times.Once);
        userManager.Verify(m => m.IsInRoleAsync(user, role), Times.Once);
    }
}
