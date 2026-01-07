using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Services;

namespace Vitarish.Interfaces.Base.Tests.Services;

/// <summary>
/// Unit tests for the BottomNavService implementation.
/// </summary>
public sealed class BottomNavServiceTests
{
    [Fact]
    public void Default_state_has_all_buttons_except_back()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Current.Home.Visible.Should().BeTrue();
        service.Current.Home.Enabled.Should().BeTrue();
        service.Current.Search.Visible.Should().BeTrue();
        service.Current.Search.Enabled.Should().BeTrue();
        service.Current.Profile.Visible.Should().BeTrue();
        service.Current.Profile.Enabled.Should().BeTrue();
        service.Current.Back.Visible.Should().BeFalse();
        service.Current.Back.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Default_home_button_navigates_to_root()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        await service.Current.Home.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/");
    }

    [Fact]
    public async Task Default_search_button_navigates_to_search_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        await service.Current.Search.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/search");
    }

    [Fact]
    public async Task Default_profile_button_navigates_to_profile_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        await service.Current.Profile.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/profile");
    }

    [Fact]
    public async Task Custom_defaults_are_used_for_navigation()
    {
        TestNavigationManager navigationManager = new();
        BottomNavDefaults customDefaults = new("/dashboard", "/user", "/find");
        BottomNavService service = new(navigationManager, customDefaults);

        await service.Current.Home.OnClick();
        await service.Current.Search.OnClick();
        await service.Current.Profile.OnClick();

        navigationManager.History.Should().HaveCount(3);
        navigationManager.History[0].Should().Be("http://localhost/dashboard");
        navigationManager.History[1].Should().Be("http://localhost/find");
        navigationManager.History[2].Should().Be("http://localhost/user");
    }

    [Fact]
    public void Apply_with_home_override_changes_home_destination()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(HomeUriOverride: "/custom-home"));

        service.Current.Home.OnClick().Wait();
        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/custom-home");
    }

    [Fact]
    public void Apply_with_back_uri_makes_back_button_visible()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(Visible: true, Uri: "/previous")
        ));

        service.Current.Back.Visible.Should().BeTrue();
        service.Current.Back.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Back_button_with_uri_navigates_correctly()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(Visible: true, Uri: "/previous")
        ));

        await service.Current.Back.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/previous");
    }

    [Fact]
    public async Task Back_button_with_action_executes_custom_logic()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        bool actionExecuted = false;

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(
                Visible: true,
                Action: () =>
                {
                    actionExecuted = true;
                    return Task.CompletedTask;
                }
            )
        ));

        await service.Current.Back.OnClick();

        actionExecuted.Should().BeTrue();
        navigationManager.History.Should().BeEmpty(); // Action takes precedence, no navigation
    }

    [Fact]
    public void Back_button_action_takes_precedence_over_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        bool actionExecuted = false;

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(
                Visible: true,
                Uri: "/should-not-navigate",
                Action: () =>
                {
                    actionExecuted = true;
                    return Task.CompletedTask;
                }
            )
        ));

        service.Current.Back.OnClick().Wait();

        actionExecuted.Should().BeTrue();
        navigationManager.History.Should().BeEmpty();
    }

    [Fact]
    public void Back_button_visible_false_keeps_button_hidden()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(Visible: false, Uri: "/previous")
        ));

        service.Current.Back.Visible.Should().BeFalse();
    }

    [Fact]
    public void Back_button_visible_true_without_action_or_uri_is_disabled()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(
            Back: new BackTarget(Visible: true)
        ));

        service.Current.Back.Visible.Should().BeTrue();
        service.Current.Back.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Search_with_custom_action_executes_action()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        bool actionExecuted = false;

        service.Apply(new BottomNavOptions(
            Search: new SearchAction(
                Action: () =>
                {
                    actionExecuted = true;
                    return Task.CompletedTask;
                }
            )
        ));

        await service.Current.Search.OnClick();

        actionExecuted.Should().BeTrue();
        navigationManager.History.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_with_custom_uri_navigates_to_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(
            Search: new SearchAction(Uri: "/custom-search")
        ));

        await service.Current.Search.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/custom-search");
    }

    [Fact]
    public void Search_action_takes_precedence_over_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        bool actionExecuted = false;

        service.Apply(new BottomNavOptions(
            Search: new SearchAction(
                Uri: "/should-not-navigate",
                Action: () =>
                {
                    actionExecuted = true;
                    return Task.CompletedTask;
                }
            )
        ));

        service.Current.Search.OnClick().Wait();

        actionExecuted.Should().BeTrue();
        navigationManager.History.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_without_configuration_uses_default_uri()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions()); // No search configuration

        await service.Current.Search.OnClick();

        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/search");
    }

    [Fact]
    public void Apply_notifies_changed_subscribers()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        int changedCount = 0;
        service.Changed += () => changedCount++;

        service.Apply(new BottomNavOptions(HomeUriOverride: "/new-home"));

        changedCount.Should().Be(1);
    }

    [Fact]
    public void Apply_with_same_state_does_not_notify()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        int changedCount = 0;
        service.Changed += () => changedCount++;

        service.Apply(null); // Same as default

        changedCount.Should().Be(0);
    }

    [Fact]
    public void ResetToDefaults_restores_initial_state()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        service.Apply(new BottomNavOptions(
            HomeUriOverride: "/custom",
            Back: new BackTarget(Visible: true, Uri: "/back")
        ));

        service.ResetToDefaults();

        service.Current.Home.OnClick().Wait();
        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/"); // Back to default
        service.Current.Back.Visible.Should().BeFalse(); // Back hidden again
    }

    [Fact]
    public void LocationChanged_automatically_resets_to_defaults()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        service.Apply(new BottomNavOptions(
            HomeUriOverride: "/custom",
            Back: new BackTarget(Visible: true, Uri: "/back")
        ));

        navigationManager.TriggerLocationChanged("/another-page");

        service.Current.Home.OnClick().Wait();
        navigationManager.History.Should().Contain("http://localhost/"); // Reset to default
        service.Current.Back.Visible.Should().BeFalse();
    }

    [Fact]
    public void Profile_button_always_visible_and_enabled()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        // Try various configurations
        service.Apply(new BottomNavOptions(HomeUriOverride: "/custom"));
        service.Current.Profile.Visible.Should().BeTrue();
        service.Current.Profile.Enabled.Should().BeTrue();

        service.Apply(new BottomNavOptions(Back: new BackTarget(Visible: true, Uri: "/back")));
        service.Current.Profile.Visible.Should().BeTrue();
        service.Current.Profile.Enabled.Should().BeTrue();

        service.ResetToDefaults();
        service.Current.Profile.Visible.Should().BeTrue();
        service.Current.Profile.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_unhooks_location_changed_handler()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);
        service.Apply(new BottomNavOptions(HomeUriOverride: "/custom"));

        service.Dispose();
        navigationManager.TriggerLocationChanged("/another-page");

        // State should not have reset because handler was removed
        service.Current.Home.OnClick().Wait();
        navigationManager.History.Should().ContainSingle()
            .Which.Should().Be("http://localhost/custom"); // Still custom, not reset
    }

    [Fact]
    public void Multiple_configuration_changes_accumulate_correctly()
    {
        TestNavigationManager navigationManager = new();
        BottomNavService service = new(navigationManager);

        service.Apply(new BottomNavOptions(HomeUriOverride: "/home1"));
        service.Current.Home.OnClick().Wait();

        service.Apply(new BottomNavOptions(HomeUriOverride: "/home2"));
        service.Current.Home.OnClick().Wait();

        service.Apply(new BottomNavOptions(HomeUriOverride: "/home3"));
        service.Current.Home.OnClick().Wait();

        navigationManager.History.Should().HaveCount(3);
        navigationManager.History[0].Should().Be("http://localhost/home1");
        navigationManager.History[1].Should().Be("http://localhost/home2");
        navigationManager.History[2].Should().Be("http://localhost/home3");
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
