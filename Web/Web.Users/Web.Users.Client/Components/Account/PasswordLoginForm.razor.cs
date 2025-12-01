namespace Vitarish.Web.Users.Client.Components.Account;

public partial class PasswordLoginForm : CommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
}
