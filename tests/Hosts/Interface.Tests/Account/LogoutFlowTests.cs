namespace Vitarish.Interface.Tests.Account;

using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Moq;
using Vitarish.Interface.Presentation.Pages.Account;
using Vitarish.Interface.Tests.TestInfrastructure;

public class LogoutFlowTests
{
    [Fact]
    public async Task LogoutUser_SignsOutAndStoresLogoutContext()
    {
        using var fixture = new ComponentFixture();
        var request = new LogoutRequest("client", new LogoutMessage())
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
