using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Accounts.Infrastructure.Services;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class UserProfileCultureInfoProviderTests
{
    [Fact]
    public async Task UserProfileCultureInfoProvider_ReturnsDefault_WhenNoProfile()
    {
        Mock<IUserService> userService = new();
        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }))
        };
        Mock<IHttpContextAccessor> accessor = new();
        Mock<IJSRuntime> jsRuntime = new();

        accessor.SetupGet(a => a.HttpContext).Returns(context);
        userService.Setup(service => service.GetCultureAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        UserProfileCultureInfoProvider sut = new(userService.Object, accessor.Object, jsRuntime.Object);

        CultureInfo? culture = await sut.GetRequestedCultureAsync();

        culture.Should().BeNull();
    }

    [Fact]
    public async Task UserProfileCultureInfoProvider_ReturnsUserCulture_WhenSet()
    {
        Mock<IUserService> userService = new();
        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }))
        };
        Mock<IHttpContextAccessor> accessor = new();
        Mock<IJSRuntime> jsRuntime = new();

        accessor.SetupGet(a => a.HttpContext).Returns(context);
        userService.Setup(service => service.GetCultureAsync(It.IsAny<string>())).ReturnsAsync("en-US");

        UserProfileCultureInfoProvider sut = new(userService.Object, accessor.Object, jsRuntime.Object);

        CultureInfo? culture = await sut.GetRequestedCultureAsync();

        culture.Should().NotBeNull();
        culture!.Name.Should().Be("en-US");
    }
}
