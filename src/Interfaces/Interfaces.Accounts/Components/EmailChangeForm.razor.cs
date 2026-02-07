namespace Vitarish.Interfaces.Accounts.Components;

public partial class EmailChangeForm : IdentityCommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    private string? Email = null;
    private string? error = null;
    private bool hasRendered = false;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
        }
    }

    private async void ValidSendCodeSubmit(SendEmailCodeModel model)
    {
        if (User is null)
        {
            error = Localizer["UserNotFound"];
            NotifyStateChanged();
            return;
        }
        bool emailInUse = await UserService.CheckUserEmailInUseAsync(model.Email, User.Id);
        if (emailInUse)
        {
            error = Localizer["InvalidEmail"];
        }
        else
        {
            await UserService.SendEmailCode(User.Id, model.Email);
            Email = model.Email;
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (hasRendered)
        {
            StateHasChanged();
        }
    }
}
