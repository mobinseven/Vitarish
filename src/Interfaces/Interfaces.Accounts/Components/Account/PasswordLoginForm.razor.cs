using Vitarish.Interfaces.Accounts.Components.Base;
using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components.Account;

public partial class PasswordLoginForm : IdentityCommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
}
