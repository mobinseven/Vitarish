namespace Vorn.Aaas.Interface.Presentation.Components.Account;

public partial class EmailVerificationSend : CommonBase
{
    [Parameter] public EventCallback<SendEmailCodeModel> OnValidSubmit { get; set; }
    string Email = string.Empty;
    private readonly string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
    bool isValid => Regex.IsMatch(Email, emailPattern);
    bool submitting;
    async void ValidSubmit()
    {
        submitting = true;
        await InvokeAsync(StateHasChanged);
        await OnValidSubmit.InvokeAsync(new SendEmailCodeModel { Email = Email });
    }

}
