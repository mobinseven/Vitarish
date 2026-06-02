namespace Vitarish.Interfaces.Accounts.Components;

public partial class EmailVerificationSend : IdentityCommonBase
{
    [Parameter] public EventCallback<SendEmailCodeModel> OnValidSubmit { get; set; }
    private string Email = string.Empty;

    private readonly string emailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";
    private bool IsValid => Regex.IsMatch(Email, emailPattern);
    private async void ValidSubmit()
    {
        await InvokeAsync(StateHasChanged);
        await OnValidSubmit.InvokeAsync(new SendEmailCodeModel {  Email = Email });
    }

}
