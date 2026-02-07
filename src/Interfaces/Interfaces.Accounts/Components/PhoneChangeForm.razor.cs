using Vitarish.Interfaces.Accounts.Models;

namespace Vitarish.Interfaces.Accounts.Components;

public partial class PhoneChangeForm : IdentityCommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private string? PhoneNumber = null;
    private string? error = null;
    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        await base.OnInitializedAsync();
        bool phoneNumberInUse = await UserService.CheckUserPhoneNumberInUseAsync(model.PhoneNumber, User?.Id);
        if (phoneNumberInUse)
        {
            error = Localizer["InvalidPhoneNumber"];
        }
        else
        {
            await UserService.SendSmsCode(User.Id, model.PhoneNumber);
            error = null;
            PhoneNumber = model.PhoneNumber;
        }
        await InvokeAsync(StateHasChanged);
    }

}
