namespace Vitarish.Main.Pages.Account.Manage;

public partial class Phone
{

    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected User? User;
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["PhoneChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";

}
