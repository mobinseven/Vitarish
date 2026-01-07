using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Vitarish.Interfaces.Base.Common;

/// <summary>
/// Bottom navigation bar component with floating pill design and radial Create menu.
/// </summary>
public partial class MudBottomNavBar : ComponentBase, IDisposable
{
    [Inject] private IBottomNavService BottomNavService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// The currently active navigation button.
    /// </summary>
    [Parameter]
    public NavButtonId? Active { get; set; }

    /// <summary>
    /// The radial actions to show when the Create menu is expanded.
    /// </summary>
    [Parameter]
    public IReadOnlyList<RadialAction>? CreateActions { get; set; }

    /// <summary>
    /// Whether the Create menu is enabled.
    /// </summary>
    [Parameter]
    public bool EnableCreateMenu { get; set; } = true;

    /// <summary>
    /// Event raised when the Create menu is toggled.
    /// </summary>
    [Parameter]
    public EventCallback<bool> CreateMenuToggled { get; set; }

    private bool _createMenuOpen;
    private BottomNavState State => BottomNavService.Current;
    private bool _disposed;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        BottomNavService.Changed += OnNavStateChanged;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        
        // Determine active state from current URL if not explicitly set
        if (Active == null)
        {
            var uri = new Uri(NavigationManager.Uri);
            var path = uri.AbsolutePath.ToLowerInvariant();
            
            if (path == "/" || path.StartsWith("/home"))
                Active = NavButtonId.Home;
            else if (path.StartsWith("/search"))
                Active = NavButtonId.Search;
            else if (path.StartsWith("/profile"))
                Active = NavButtonId.Profile;
        }
    }

    private void OnNavStateChanged()
    {
        if (!_disposed)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private async Task ToggleCreateMenu()
    {
        _createMenuOpen = !_createMenuOpen;
        await CreateMenuToggled.InvokeAsync(_createMenuOpen);
    }

    private async Task CloseCreateMenu()
    {
        if (_createMenuOpen)
        {
            _createMenuOpen = false;
            await CreateMenuToggled.InvokeAsync(false);
        }
    }

    private async Task HandleRadialActionClick(RadialAction action)
    {
        await action.OnClick();
        await CloseCreateMenu();
    }

    private async Task HandleHomeClick()
    {
        await State.Home.OnClick();
        await CloseCreateMenu();
    }

    private async Task HandleSearchClick()
    {
        await State.Search.OnClick();
        await CloseCreateMenu();
    }

    private async Task HandleBackClick()
    {
        await State.Back.OnClick();
        await CloseCreateMenu();
    }

    private async Task HandleProfileClick()
    {
        await State.Profile.OnClick();
        await CloseCreateMenu();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            BottomNavService.Changed -= OnNavStateChanged;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Navigation button identifiers.
/// </summary>
public enum NavButtonId
{
    Home,
    Search,
    Back,
    Profile
}
