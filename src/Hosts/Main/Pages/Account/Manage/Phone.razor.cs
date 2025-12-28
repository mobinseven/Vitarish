using Duende.IdentityServer.Extensions;

namespace Vitarish.Main.Pages.Account.Manage;

public partial class Phone
{

    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected User? User;
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["PhoneChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";
    protected override async Task OnInitializedAsync()
    {
        string? userId = HttpContext?.User.GetSubjectId();
        User = await UserService.GetUserAsync(userId);
        StateHasChanged();
    }

}
