namespace Vitarish.Main.Components;

public class LoginBase : IdentityBase
{
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (HttpContext?.User.Identity?.IsAuthenticated ?? false)
        {
            NavigationManager.NavigateTo("/");
        }
    }
}
