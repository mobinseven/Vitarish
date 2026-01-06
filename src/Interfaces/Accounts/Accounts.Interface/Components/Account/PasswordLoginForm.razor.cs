using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Base;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PasswordLoginForm : IdentityCommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
}
