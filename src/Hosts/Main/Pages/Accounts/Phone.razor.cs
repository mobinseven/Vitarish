using Duende.IdentityServer.Extensions;

namespace Vitarish.Main.Pages.Accounts;

public partial class Phone
{

    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["PhoneChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";

}
