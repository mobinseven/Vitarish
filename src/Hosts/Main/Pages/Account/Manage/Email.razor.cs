namespace Vitarish.Main.Pages.Account.Manage;

public partial class Email
{
    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected User? User;
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["EmailChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";
    protected override async Task OnInitializedAsync()
    {
        User = await UserService.GetUserAsync();
        StateHasChanged();
    }
}
