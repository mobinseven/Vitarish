using Accounts.Interface.Tests.TestInfrastructure;
using Duende.IdentityServer.Models;
using Vitarish.Main.Pages.Account;

namespace Accounts.Interface.Tests.Account;

public class LogoutFlowTests
{
    [Fact]
    public async Task LogoutUser_SignsOutAndStoresLogoutContext()
    {
        using var fixture = new ComponentFixture();
        LogoutRequest request = new("client", new LogoutMessage())
        {
            PostLogoutRedirectUri = "https://post-logout",
            SignOutIFrameUrl = "https://iframe",
        };

        fixture.Mocks.IdentityInteraction
            .Setup(i => i.GetLogoutContextAsync("logout-123"))
            .ReturnsAsync(request);

        var logout = new TestableLogout();
        ComponentTestHelper.SetProperty(logout, nameof(TestableLogout.LogoutIdParameter), "logout-123", typeof(TestableLogout));
        fixture.ConfigureCommonComponent(logout);

        fixture.Mocks.SignInManager.Context.User = fixture.Mocks.HttpContext.User;
        Assert.True(fixture.Mocks.SignInManager.IsSignedIn(fixture.Mocks.SignInManager.Context.User));

        await ComponentTestHelper.InvokeAsync(logout, "LogoutUser", typeof(Logout));

        fixture.Mocks.IdentityInteraction.Verify(i => i.GetLogoutContextAsync("logout-123"), Times.Once);
        Assert.True(fixture.Mocks.SignInManager.SignOutCalled);
        Assert.True(logout.LoggedOut);
        Assert.Equal("https://post-logout", logout.PostLogoutRedirectUri);
        Assert.Equal("https://iframe", logout.SignOutIframeUrl);
    }
}
