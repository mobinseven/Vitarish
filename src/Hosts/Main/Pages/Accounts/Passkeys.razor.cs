using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Main.Pages.Accounts;

public partial class Passkeys
{
    [Inject] private IWebAuthnService WebAuthnService { get; set; } = default!;
    [Inject] private ILogger<Passkeys> Logger { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<WebAuthnCredentialDto> Credentials { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsRegistering { get; set; }
    private bool IsDeletingCredential { get; set; }
    private bool IsSupported { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            IsSupported = true;
            await LoadCredentialsAsync();
            StateHasChanged();
        }
    }

    private async Task LoadCredentialsAsync()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            if (User?.Id == null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            IEnumerable<WebAuthnCredentialDto> credentials = await WebAuthnService.ListCredentialsAsync(User.Id);
            Credentials = [.. credentials];
        }
        catch (Exception)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeletePasskeyAsync(WebAuthnCredentialDto credentialDto)
    {
        try
        {
            if (User?.Id == null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            bool deleted = await WebAuthnService.DeleteCredentialAsync(credentialDto.Id, User.Id);

            if (deleted)
            {
                _ = Snackbar.Add(Localizer["PasskeyDeleted"], Severity.Success);
                await LoadCredentialsAsync();
            }
            else
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
            }
        }
        catch (Exception)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
    }

    private async Task RegisterPasskeyAsync()
    {
        try
        {
            if (User?.Id == null)
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            string optionsJson = await WebAuthnService.BeginRegistrationAsync(User.Id);
            string attestationJson = await JSRuntime.InvokeAsync<string>("webauthn.register", optionsJson);
            await WebAuthnService.CompleteRegistrationAsync(User.Id, attestationJson);
            _ = Snackbar.Add(Localizer["PasskeyRegistered"], Severity.Success);
            await LoadCredentialsAsync();
        }
        catch (Exception)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
    }
}
