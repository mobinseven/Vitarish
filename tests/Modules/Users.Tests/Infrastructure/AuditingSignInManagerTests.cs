using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using Users.Tests.TestHelpers;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Service;

namespace Users.Tests.Infrastructure;

public class AuditingSignInManagerTests
{
    [Fact]
    public async Task PasswordSignInAsync_WhenUserMissing_LogsWarning()
    {
        TestLogger<AuditingSignInManager> logger = new();
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByNameAsync("missing")).ReturnsAsync((User?)null);

        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        Mock<IHttpContextAccessor> contextAccessor = new();
        _ = contextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        Mock<IUserClaimsPrincipalFactory<User>> claimsFactory = new();
        _ = claimsFactory.Setup(f => f.CreateAsync(It.IsAny<User>())).ReturnsAsync(new ClaimsPrincipal());

        Mock<IAuthenticationSchemeProvider> schemes = new();
        Mock<IUserConfirmation<User>> confirmation = new();

        AuditingSignInManager manager = new(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            Options.Create(new IdentityOptions()),
            logger,
            schemes.Object,
            confirmation.Object);

        SignInResult result = await manager.PasswordSignInAsync("missing", "password", false, false);

        _ = result.Succeeded.Should().BeFalse();
        _ = logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("Authentication failure"));
    }
}
