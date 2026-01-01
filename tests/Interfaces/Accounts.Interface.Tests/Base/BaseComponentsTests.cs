using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using System.Security.Claims;
using Vitarish.Accounts.Interface.Components.Base;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.Base;

public class BaseComponentsTests
{
    [Fact]
    public async Task AccountLayout_RendersChildContent()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = new();

        IRenderedComponent<AccountLayout> cut = context.Render<AccountLayout>(parameters => parameters
            .AddCascadingValue(httpContext)
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Hello"))));

        Assert.Contains("Hello", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountLayout_ShowsNavWhenAuthenticated()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = new();

        IRenderedComponent<AccountLayout> cut = context.Render<AccountLayout>(parameters => parameters
            .AddCascadingValue(httpContext)
            .Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "Body"))));

        Assert.DoesNotContain("MudProgressLinear", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Home", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityBase_ProvidesCascadingAuthState()
    {
        await using InterfaceTestContext context = new();
        TestAuthenticatedStateProvider authProvider = new("user-1");
        _ = context.Services.AddSingleton<AuthenticationStateProvider>(authProvider);

        IRenderedComponent<TestIdentityComponent> cut = context.Render<TestIdentityComponent>();

        Assert.NotNull(cut.Instance.AuthenticatedUser);
        Assert.Equal("user-1", cut.Instance.AuthenticatedUser?.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task CommonBase_InitializesServices()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1", UserName = "alice" };

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        IRenderedComponent<TestCommonComponent> cut = context.Render<TestCommonComponent>();

        Assert.NotNull(cut.Instance.SnackbarService);
        Assert.NotNull(cut.Instance.LocalizerService);
        Assert.NotNull(cut.Instance.NavigationService);
        Assert.Equal(user.Id, cut.Instance.ExposedUser?.Id);
    }

    [Fact]
    public async Task Countdown_UpdatesDisplayedValue_OverTime()
    {
        await using InterfaceTestContext context = new();

        IRenderedComponent<Countdown> cut = context.Render<Countdown>(parameters => parameters
            .Add(p => p.Duration, TimeSpan.FromSeconds(2))
            .Add(p => p.AutoStart, true)
            .Add(p => p.ShowControls, false));

        string initialValue = cut.Find("span.time").TextContent;

        await Task.Delay(TimeSpan.FromSeconds(1.2));

        cut.WaitForAssertion(() =>
        {
            string updatedValue = cut.Find("span.time").TextContent;
            Assert.NotEqual(initialValue, updatedValue);
        });
    }

    private sealed class TestIdentityComponent : IdentityBase
    {
        public ClaimsPrincipal? AuthenticatedUser { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            AuthenticatedUser = state.User;
            await base.OnInitializedAsync();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private sealed class TestCommonComponent : CommonBase
    {
        public User? ExposedUser => User;
        public ISnackbar SnackbarService => Snackbar;
        public IStringLocalizer LocalizerService => Localizer;
        public NavigationManager NavigationService => NavigationManager;

        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private sealed class TestAuthenticatedStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState state;

        public TestAuthenticatedStateProvider(string userId)
        {
            ClaimsPrincipal principal = new(new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim(ClaimTypes.Name, "TestUser"),
            }, "Cookies"));
            state = new AuthenticationState(principal);
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
    }
}
