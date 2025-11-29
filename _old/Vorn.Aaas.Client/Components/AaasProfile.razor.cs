using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
namespace Vorn.Aaas.Client;

public partial class AaasProfile
{

    [Parameter, EditorRequired] public RenderFragment<AuthenticationState?> SubmitButton { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }
    private string? currentUrl;
    private AuthenticationState? authState;
    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask is null)
        {
            return;
        }
        authState = await AuthStateTask;
        if (!authState.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }
        currentUrl = NavigationManager.ToAbsoluteUri(NavigationManager.ToBaseRelativePath(NavigationManager.Uri)).AbsoluteUri;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToAbsoluteUri(NavigationManager.ToBaseRelativePath(e.Location)).AbsolutePath;
        StateHasChanged();
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;

}
