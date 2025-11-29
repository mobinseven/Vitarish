namespace Vorn.Aaas.Interface.Presentation.Pages.Account.Manage;

public partial class Phone
{

    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected User? User;
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["PhoneChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";
    protected override async Task OnInitializedAsync()
    {
        string? id = HttpContext?.User.FindFirstValue("sub");
        if (id is not null)
        {
            User = await UserService.GetUserAsync(id);
        }
    }

}
