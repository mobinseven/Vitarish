using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components.Account;

public partial class PhoneVerificationVerify
{
    [Parameter] public string Action { get; set; } = "/Account/Login/Phone/Submit";
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    [Parameter] public User User { get; set; } = default!;
    [Parameter] public string PhoneNumber { get; set; } = string.Empty;

    protected VerifySmsCodeModel VerifyCodeInput { get; private set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (User is null)
        {
            throw new ArgumentNullException(nameof(User));
        }
        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            throw new ArgumentNullException(nameof(PhoneNumber));
        }
        VerifyCodeInput.User = User;
        VerifyCodeInput.PhoneNumber = PhoneNumber;
    }
}
