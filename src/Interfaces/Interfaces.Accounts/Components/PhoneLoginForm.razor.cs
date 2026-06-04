namespace Vitarish.Interfaces.Accounts.Components;

public partial class PhoneLoginForm : IdentityCommonBase
{
    [Inject] private IVerificationSmsRequestShield VerificationSmsRequestShield { get; set; } = default!;
    [Inject] private ILogger<PhoneLoginForm> Logger { get; set; } = default!;

    private new User? User = null;
    private string? PhoneNumber = null;

    private async Task ValidSendCodeSubmit(SendSmsCodeModel model)
    {
        ClaimsPrincipal claimsPrincipal = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (!claimsPrincipal.Identity?.IsAuthenticated ?? true)
        {
            User = await SmsAuthenticationService.GetOrRegisterUser(model.PhoneNumber);
            string shieldKey = VerificationSmsShieldKeys.ForPhoneLogin(model.PhoneNumber);
            VerificationSmsShieldDecision shieldDecision = VerificationSmsRequestShield.TryAcquireSendSlot(shieldKey);
            if (shieldDecision.CanSendSms)
            {
                try
                {
                    await SmsAuthenticationService.SendCode(User, model.PhoneNumber);
                }
                catch
                {
                    VerificationSmsRequestShield.ReleaseSendSlot(shieldKey);
                    throw;
                }
            }
            else
            {
                Logger.LogInformation(
                    "Suppressed duplicate phone login verification SMS request. RemainingCooldownSeconds: {RemainingCooldownSeconds}.",
                    Math.Ceiling(shieldDecision.RemainingCooldown.TotalSeconds));
            }

            PhoneNumber = model.PhoneNumber;
        }
        else
        {
            throw new InvalidOperationException("Authenticated user accessing a non-authenticated user form");
        }

        StateHasChanged();
    }
}
