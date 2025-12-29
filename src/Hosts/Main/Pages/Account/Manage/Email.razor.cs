using Duende.IdentityServer.Extensions;

namespace Vitarish.Main.Pages.Account.Manage;

public partial class Email
{
    [SupplyParameterFromQuery]
    protected string? FailureMessage { get; set; }
    protected string failureUrl => NavigationManager.GetUriWithQueryParameters(NavigationManager.Uri, new Dictionary<string, object?> { ["FailureMessage"] = Localizer["EmailChangeFailed"].Value });
    protected string returnUrl => ReturnUrl ?? "/Account/Manage";
}
