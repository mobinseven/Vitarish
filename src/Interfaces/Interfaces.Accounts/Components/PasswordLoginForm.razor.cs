using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components;

public partial class PasswordLoginForm : IdentityCommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
}
