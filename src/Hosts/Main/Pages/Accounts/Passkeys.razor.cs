using System.Text.Json;
using Vitarish.Accounts.Interface.Abstractions;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Pages.Accounts;

public partial class Passkeys
{
    [Inject] private IWebAuthnService WebAuthnService { get; set; } = default!;
    [Inject] private IWebAuthnInterop WebAuthnInterop { get; set; } = default!;
    [Inject] private ILogger<Passkeys> Logger { get; set; } = default!;

    private List<WebAuthnCredentialDto> Credentials { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsRegistering { get; set; }
    private bool IsDeletingCredential { get; set; }
    private bool IsSupported { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await WebAuthnInterop.InitializeAsync();
                IsSupported = await WebAuthnInterop.IsWebAuthnSupportedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to initialize WebAuthn support check");
                IsSupported = false;
            }

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

    private async Task RegisterPasskeyAsync()
    {
        IsRegistering = true;
        StateHasChanged();

        try
        {
            if (User?.Id == null)
            {
                _ = Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
                return;
            }

            // Step 1: Begin registration - get options from service
            string optionsJson = await WebAuthnService.BeginRegistrationAsync(User.Id);
            JsonDocument options = JsonDocument.Parse(optionsJson);

            // Step 2: Create credential using authenticator via the interop service
            object credentialResponse = await WebAuthnInterop.CreateCredentialAsync(options.RootElement);

            // Step 3: Complete registration - send attestation to service
            string credentialResponseJson = JsonSerializer.Serialize(credentialResponse);
            string credentialId = await WebAuthnService.CompleteRegistrationAsync(User.Id, credentialResponseJson);

            _ = Snackbar.Add(Localizer["PasskeyRegistered"], Severity.Success);
            await LoadCredentialsAsync();
        }
        catch (WebAuthnUserCancelledException ex)
        {
            Logger.LogInformation(ex, "WebAuthn registration cancelled by user");
            _ = Snackbar.Add(Localizer["PasskeyRegistrationCancelled"], Severity.Warning);
        }
        catch (WebAuthnNotSupportedException ex)
        {
            Logger.LogWarning(ex, "WebAuthn not supported during registration");
            _ = Snackbar.Add(Localizer["PasskeyNotSupported"], Severity.Error);
        }
        catch (WebAuthnException ex)
        {
            Logger.LogError(ex, "WebAuthn registration failed");
            _ = Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during passkey registration");
            _ = Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
        }
        finally
        {
            IsRegistering = false;
            StateHasChanged();
        }
    }

    private async Task DeletePasskeyAsync(WebAuthnCredentialDto credentialDto)
    {
        DialogParameters parameters = new()
            {
                { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
                { nameof(ConfirmDialog.ButtonText), Localizer["Delete", credentialDto.FriendlyName].Value },
                { nameof(ConfirmDialog.Color), Color.Error }
            };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        IsDeletingCredential = true;
        StateHasChanged();

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
        finally
        {
            IsDeletingCredential = false;
            StateHasChanged();
        }
    }

}
