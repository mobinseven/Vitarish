using System.Text.Json;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Pages.Accounts;

public partial class Passkeys : IAsyncDisposable
{
    private List<PasskeyCredentialDto> Credentials { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsRegistering { get; set; }
    private bool IsDeletingCredential { get; set; }
    private bool IsSupported { get; set; }

    private IJSObjectReference? _jsModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await Js.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./_content/Vitarish.Accounts.Interface/Components/Account/WebAuthnInterop.razor.js");
                IsSupported = await _jsModule.InvokeAsync<bool>("isWebAuthnSupported");
            }
            catch (Exception ex)
            {
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
        using HttpClient httpClient = HttpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

        HttpResponseMessage response = await httpClient.GetAsync("/api/webauthn/credentials");
        if (response.IsSuccessStatusCode)
        {
            List<PasskeyCredentialDto>? credentials = await response.Content.ReadFromJsonAsync<List<PasskeyCredentialDto>>();
            Credentials = credentials ?? [];
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
        IsLoading = false;
    }

    private async Task RegisterPasskeyAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        IsRegistering = true;
        StateHasChanged();
        using HttpClient httpClient = HttpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

        // Step 1: Begin registration - get options from server
        HttpResponseMessage beginResponse = await httpClient.PostAsync("/api/webauthn/register/begin", null);

        if (!beginResponse.IsSuccessStatusCode)
        {
            _ = Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
            return;
        }

        JsonDocument options = await beginResponse.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("Failed to parse registration options");

        // Step 2: Create credential using authenticator
        object credentialResponse = await _jsModule.InvokeAsync<object>(
            "createCredential",
            options.RootElement);

        // Step 3: Complete registration - send attestation to server
        HttpResponseMessage completeResponse = await httpClient.PostAsJsonAsync(
            "/api/webauthn/register/complete",
            credentialResponse);

        if (completeResponse.IsSuccessStatusCode)
        {
            _ = Snackbar.Add(Localizer["PasskeyRegistered"], Severity.Success);
            await LoadCredentialsAsync();
        }
        else
        {
            _ = Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
        }

        IsRegistering = false;
        StateHasChanged();
    }

    private async Task DeletePasskeyAsync(PasskeyCredentialDto credentialDto)
    {

        DialogParameters parameters = new()
            {
                { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
                { nameof(ConfirmDialog.ButtonText), Localizer["Delete", credentialDto.FriendlyName].Value },
                { nameof(ConfirmDialog.Color), Color.Error }
            };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (result.Canceled)
        {
            return;
        }

        IsDeletingCredential = true;
        StateHasChanged();
        Guid credentialId = credentialDto.Id;
        try
        {
            using HttpClient httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

            HttpResponseMessage response = await httpClient.DeleteAsync($"/api/webauthn/credentials/{credentialId}");

            if (response.IsSuccessStatusCode)
            {
                _ = Snackbar.Add(Localizer["PasskeyDeleted"], Severity.Success);
                await LoadCredentialsAsync();
            }
            else
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Error);
            }
        }
        catch (Exception ex)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
        finally
        {
            IsDeletingCredential = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}

public record PasskeyCredentialDto
{
    public Guid Id { get; init; }
    public string? FriendlyName { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? Transports { get; init; }
}
