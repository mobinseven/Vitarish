using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components.Account;

public partial class EmailVerificationVerify
{
    [Parameter] public string VerificationEndpoint { get; set; } = "/Account/Login/Email/Submit";
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    [Parameter] public User User { get; set; } = default!;
    [Parameter] public string Email { get; set; } = string.Empty;

    protected VerifyEmailCodeModel VerifyCodeInput { get; private set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (User is null)
        {
            throw new ArgumentNullException(nameof(User));
        }
        if (string.IsNullOrWhiteSpace(Email))
        {
            throw new ArgumentNullException(nameof(Email));
        }
        VerifyCodeInput.User = User;
        VerifyCodeInput.Email = Email;
    }
}
