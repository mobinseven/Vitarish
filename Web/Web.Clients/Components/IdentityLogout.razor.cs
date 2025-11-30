using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Vitarish.Web.Clients;

/// <summary>
/// Component for rendering a logout form.
/// </summary>
public partial class IdentityLogout : IDisposable
{
    /// <summary>
    /// The submit button content (receives authentication state).
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment<AuthenticationState?> SubmitButton { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    private string? currentUrl;
    private AuthenticationState? authState;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask is null)
        {
            return;
        }

        authState = await AuthStateTask;
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    /// <inheritdoc />
    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
}
