using Microsoft.AspNetCore.Components.Authorization;
namespace Vitarish.Clients.Interface.Identity.Components;

public partial class Logout
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
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;

}
