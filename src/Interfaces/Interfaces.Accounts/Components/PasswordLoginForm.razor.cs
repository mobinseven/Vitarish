namespace Vitarish.Interfaces.Accounts.Components;

public partial class PasswordLoginForm : IdentityCommonBase
{
    [SupplyParameterFromForm] private PasswordLoginModel Model { get; set; } = new();
    private bool IsValid => Model.Username?.Length > 0 && Model.Password?.Length > 0;
}
