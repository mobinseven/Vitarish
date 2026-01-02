using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Services;

namespace Vitarish.Interfaces.Base.Tests.Services;

public sealed class NavMenuServiceTests
{
    [Fact]
    public void Default_state_is_root_home_without_back_button()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);

        service.State.HomeUri.Should().Be("/");
        service.State.BackAction.Should().BeNull();
        service.State.AdditionalButtons.Should().BeNull();
    }

    [Fact]
    public void Configure_updates_state_and_notifies_subscribers()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);
        int raised = 0;
        service.StateChanged += (_, _) => raised++;
        RenderFragment additional = builder => builder.AddContent(0, "custom");
        NavMenuAction backAction = new("fa-thin fa-arrow-left", "/back");

        service.Configure("/home", backAction, additional);

        raised.Should().Be(1);
        service.State.HomeUri.Should().Be("/home");
        service.State.BackAction.Should().Be(backAction);
        service.State.AdditionalButtons.Should().NotBeNull();
    }

    [Fact]
    public async Task Configure_back_action_navigates_when_href_is_set()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);

        service.Configure(backAction: new NavMenuAction("fa-thin fa-arrow-left", "/previous"));

        await service.State.BackAction!.InvokeAsync(navigationManager);

        navigationManager.History.Should().ContainSingle().Which.Should().Be("http://localhost/previous");
    }

    [Fact]
    public async Task Configure_back_action_invokes_delegate_when_provided()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);
        bool invoked = false;

        service.Configure(backAction: new NavMenuAction("fa-thin fa-arrow-left", OnClick: () =>
        {
            invoked = true;
            return Task.CompletedTask;
        }));

        await service.State.BackAction!.InvokeAsync(navigationManager);

        invoked.Should().BeTrue();
        navigationManager.History.Should().BeEmpty();
    }

    [Fact]
    public void Reset_restores_default_state()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);
        service.Configure("/home", new NavMenuAction("icon", "/back"), builder => builder.AddContent(0, "extra"));

        service.Reset();

        service.State.Should().Be(NavMenuState.Default);
    }

    [Fact]
    public void Location_change_resets_state()
    {
        TestNavigationManager navigationManager = new();
        NavMenuService service = new(navigationManager);
        service.Configure("/home", new NavMenuAction("icon", "/back"));

        navigationManager.TriggerLocationChanged("/another");

        service.State.Should().Be(NavMenuState.Default);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public List<string> History { get; } = [];

        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri absoluteUri = ToAbsoluteUri(uri);
            History.Add(absoluteUri.ToString());
            Uri = absoluteUri.ToString();
            NotifyLocationChanged(forceLoad);
        }

        public void TriggerLocationChanged(string uri, bool forceLoad = false)
        {
            Uri = ToAbsoluteUri(uri).ToString();
            NotifyLocationChanged(forceLoad);
        }
    }
}
