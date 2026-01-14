using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Vitarish.Main.Authorization;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Host.Tests.Authorization;

public class AuthorizerTests
{
    [Fact]
    public async Task HandleRequirementAsync_CachesRoleResult()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        Mock<IUsersService> userManager = new(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        Mock<IRequestContext> mockRequestContextMock = new();
        User user = new();
        const string userId = "user-1";
        const string role = "Admin";

        _ = userManager.Setup(m => m.GetUserAsync(userId)).ReturnsAsync(user);
        _ = userManager.Setup(m => m.CheckUserRoleAsync(userId, role)).ReturnsAsync(true);

        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));

        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", userId)], "mock"));
        Authorizer handler = new(userManager.Object, cache, mockRequestContextMock.Object);
        AuthorizerRequirement requirement = new(role);

        AuthorizationHandlerContext firstContext = new([requirement], principal, null);
        await handler.HandleAsync(firstContext);

        AuthorizationHandlerContext secondContext = new([requirement], principal, null);
        await handler.HandleAsync(secondContext);

        firstContext.HasSucceeded.Should().BeTrue();
        secondContext.HasSucceeded.Should().BeTrue();
        userManager.Verify(m => m.GetUserAsync(userId), Times.Once);
        userManager.Verify(m => m.CheckUserRoleAsync(userId, role), Times.Once);
    }
}
