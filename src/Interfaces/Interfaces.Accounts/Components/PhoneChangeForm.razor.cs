namespace Vitarish.Interfaces.Accounts.Components;

public partial class PhoneChangeForm : IdentityCommonBase
{
    [Inject] private IVerificationSmsRequestShield VerificationSmsRequestShield { get; set; } = default!;
    [Inject] private ILogger<PhoneChangeForm> Logger { get; set; } = default!;

    private string? PhoneNumber = null;
    private string? error = null;
    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        await base.OnInitializedAsync();
        if (User is null)
        {
            throw new InvalidOperationException("A signed-in user is required to request a phone change verification SMS.");
        }

        string shieldKey = VerificationSmsShieldKeys.ForPhoneChange(User.Id, model.PhoneNumber);
        VerificationSmsShieldDecision shieldDecision = VerificationSmsRequestShield.TryAcquireSendSlot(shieldKey);
        if (!shieldDecision.CanSendSms)
        {
            Logger.LogInformation(
                "Suppressed duplicate phone change verification SMS request for user {UserId}. RemainingCooldownSeconds: {RemainingCooldownSeconds}.",
                User.Id,
                Math.Ceiling(shieldDecision.RemainingCooldown.TotalSeconds));
            error = null;
            PhoneNumber = model.PhoneNumber;
            await InvokeAsync(StateHasChanged);
            return;
        }

        try
        {
            bool phoneNumberInUse = await UserService.CheckUserPhoneNumberInUseAsync(model.PhoneNumber, User.Id);
            if (phoneNumberInUse)
            {
                VerificationSmsRequestShield.ReleaseSendSlot(shieldKey);
                error = Localizer["InvalidPhoneNumber"];
            }
            else
            {
                await UserService.SendSmsCode(User.Id, model.PhoneNumber);
                error = null;
                PhoneNumber = model.PhoneNumber;
            }
        }
        catch
        {
            VerificationSmsRequestShield.ReleaseSendSlot(shieldKey);
            throw;
        }
        await InvokeAsync(StateHasChanged);
    }

}
