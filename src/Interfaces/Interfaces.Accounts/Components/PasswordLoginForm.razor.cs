namespace Vitarish.Interfaces.Accounts.Components;

public partial class PasswordLoginForm : IdentityCommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
}
