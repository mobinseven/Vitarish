namespace Vitarish.Interfaces.Accounts.Components;

public partial class EmailVerificationVerify
{
    [Parameter] public User User { get; set; } = default!;
    [Parameter] public string Email { get; set; } = string.Empty;

    protected VerifyEmailCodeModel VerifyCodeInput { get; private set; } = new();

    private bool IsValid => VerifyCodeInput.Code?.Length == 6;
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
