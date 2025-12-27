namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PhoneChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private User? User = null;
    private string? PhoneNumber = null;
    private string? error = null;
    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        User = await UserService.GetUserAsync();
        bool phoneNumberInUse = await UserService.CheckUserPhoneNumberInUseAsync(model.PhoneNumber);
        if (phoneNumberInUse)
        {
            error = Localizer["InvalidPhoneNumber"];
        }
        else
        {
            await UserService.SendSmsCode(model.PhoneNumber);
            error = null;
            PhoneNumber = model.PhoneNumber;
        }
        await InvokeAsync(StateHasChanged);
    }

}
