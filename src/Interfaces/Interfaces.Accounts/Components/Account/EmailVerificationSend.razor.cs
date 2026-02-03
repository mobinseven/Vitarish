using Vitarish.Interfaces.Accounts.Components.Base;
using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components.Account;

public partial class EmailVerificationSend : IdentityCommonBase
{
    [Parameter] public EventCallback<SendEmailCodeModel> OnValidSubmit { get; set; }
    private string Email = string.Empty;
    private readonly string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
    private bool isValid => Regex.IsMatch(Email, emailPattern);
    private bool submitting;
    private async void ValidSubmit()
    {
        submitting = true;
        await InvokeAsync(StateHasChanged);
        await OnValidSubmit.InvokeAsync(new SendEmailCodeModel { Email = Email });
    }

}
