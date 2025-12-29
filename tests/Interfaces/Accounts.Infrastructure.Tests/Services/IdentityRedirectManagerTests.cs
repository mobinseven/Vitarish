using Microsoft.AspNetCore.Http;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class IdentityRedirectManagerTests
{
    [Fact]
    public void IdentityRedirectManager_RedirectTo_ReturnsRedirectResult()
    {
        RecordingNavigationManager navigationManager = new();
        Mock<ILogger<IdentityRedirectManager>> logger = new();
        IdentityRedirectManager sut = new(navigationManager, logger.Object)
        {
            HttpContext = new DefaultHttpContext()
        };

        Action act = () => sut.RedirectTo("/account");

        act.Should().Throw<InvalidOperationException>();
        navigationManager.History.Should().ContainSingle();
        navigationManager.History[0].Uri.Should().Be("http://localhost/account");
    }

    [Fact]
    public void IdentityRedirectManager_RedirectTo_HandlesNullReturnUrl()
    {
        RecordingNavigationManager navigationManager = new();
        Mock<ILogger<IdentityRedirectManager>> logger = new();
        IdentityRedirectManager sut = new(navigationManager, logger.Object)
        {
            HttpContext = new DefaultHttpContext()
        };

        Action act = () => sut.RedirectTo(null);

        act.Should().Throw<InvalidOperationException>();
        navigationManager.History.Should().ContainSingle();
        navigationManager.History[0].Uri.Should().Be("http://localhost/");
    }
}
