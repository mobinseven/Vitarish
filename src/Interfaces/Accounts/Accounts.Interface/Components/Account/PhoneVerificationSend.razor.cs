using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Localization;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PhoneVerificationSend
{

    [Inject] protected new IStringLocalizer<Strings> Localizer { get; set; } = default!;
    [Parameter] public EventCallback<SendSmsCodeModel> OnValidSubmit { get; set; }
    private string fixedPhoneNumber => PhoneNumber.ConvertToEnglishNumerals();
    private string PhoneNumber = string.Empty;
    private readonly string numberPattern = "^[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{2}[0-9\\u0660-\\u0669\\u06F0-\\u06F9]{9}$";
    private bool isValid => Regex.IsMatch(PhoneNumber, numberPattern);
    private async void ValidSubmit()
    {
        await InvokeAsync(StateHasChanged);
        await OnValidSubmit.InvokeAsync(new SendSmsCodeModel { PhoneNumber = fixedPhoneNumber });
    }

}
