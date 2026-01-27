using System.Net.Http.Json;
using System.Text.Json;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class WebAuthnManagement : IdentityCommonBase, IAsyncDisposable
{
    private List<PasskeyCredentialDto> Credentials { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsRegistering { get; set; }
    private bool IsDeletingCredential { get; set; }
    private bool IsSavingName { get; set; }
    private bool IsSupported { get; set; }
    private string? ErrorMessage { get; set; }
    private Guid? EditingCredentialId { get; set; }
    private string? EditingFriendlyName { get; set; }

    private IJSObjectReference? _jsModule;
    private ILogger<WebAuthnManagement>? _logger;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _logger = LoggerFactory.CreateLogger<WebAuthnManagement>();
    }

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
                _logger?.LogWarning(ex, "Failed to initialize WebAuthn support check");
                IsSupported = false;
            }

            await LoadCredentialsAsync();
            StateHasChanged();
        }
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient = HttpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);
        return httpClient;
    }

    private async Task LoadCredentialsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            using HttpClient httpClient = CreateHttpClient();

            HttpResponseMessage response = await httpClient.GetAsync("/api/webauthn/credentials");
            if (response.IsSuccessStatusCode)
            {
                List<PasskeyCredentialDto>? credentials = await response.Content.ReadFromJsonAsync<List<PasskeyCredentialDto>>();
                Credentials = credentials ?? [];
            }
            else
            {
                _logger?.LogWarning("Failed to load WebAuthn credentials. Status: {StatusCode}", response.StatusCode);
                ErrorMessage = Localizer["Error"];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading WebAuthn credentials");
            ErrorMessage = Localizer["Error"];
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RegisterPasskeyAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        IsRegistering = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            using HttpClient httpClient = CreateHttpClient();

            // Step 1: Begin registration - get options from server
            HttpResponseMessage beginResponse = await httpClient.PostAsync("/api/webauthn/register/begin", null);

            if (!beginResponse.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to begin WebAuthn registration. Status: {StatusCode}", beginResponse.StatusCode);
                ErrorMessage = Localizer["PasskeyRegistrationFailed"];
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
                _logger?.LogWarning("Failed to complete WebAuthn registration. Status: {StatusCode}", completeResponse.StatusCode);
                ErrorMessage = Localizer["PasskeyRegistrationFailed"];
            }
        }
        catch (JSException ex)
        {
            _logger?.LogInformation(ex, "WebAuthn registration cancelled by user");
            _ = Snackbar.Add(Localizer["PasskeyRegistrationCancelled"], Severity.Warning);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during WebAuthn registration");
            ErrorMessage = Localizer["PasskeyRegistrationFailed"];
        }
        finally
        {
            IsRegistering = false;
            StateHasChanged();
        }
    }

    private void StartEdit(Guid credentialId, string? currentName)
    {
        EditingCredentialId = credentialId;
        EditingFriendlyName = currentName ?? string.Empty;
        StateHasChanged();
    }

    private void CancelEdit()
    {
        EditingCredentialId = null;
        EditingFriendlyName = null;
        StateHasChanged();
    }

    private async Task SavePasskeyNameAsync(Guid credentialId)
    {
        IsSavingName = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            using HttpClient httpClient = CreateHttpClient();

            HttpResponseMessage response = await httpClient.PatchAsJsonAsync(
                $"/api/webauthn/credentials/{credentialId}",
                new { FriendlyName = EditingFriendlyName });

            if (response.IsSuccessStatusCode)
            {
                _ = Snackbar.Add(Localizer["PasskeyNameUpdated"], Severity.Success);
                EditingCredentialId = null;
                EditingFriendlyName = null;
                await LoadCredentialsAsync();
            }
            else
            {
                _logger?.LogWarning("Failed to update WebAuthn credential {CredentialId}. Status: {StatusCode}", credentialId, response.StatusCode);
                ErrorMessage = Localizer["Error"];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating WebAuthn credential {CredentialId}", credentialId);
            ErrorMessage = Localizer["Error"];
        }
        finally
        {
            IsSavingName = false;
            StateHasChanged();
        }
    }

    private async Task DeletePasskeyAsync(Guid credentialId)
    {
        IsDeletingCredential = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            using HttpClient httpClient = CreateHttpClient();

            HttpResponseMessage response = await httpClient.DeleteAsync($"/api/webauthn/credentials/{credentialId}");

            if (response.IsSuccessStatusCode)
            {
                _ = Snackbar.Add(Localizer["PasskeyDeleted"], Severity.Success);
                await LoadCredentialsAsync();
            }
            else
            {
                _logger?.LogWarning("Failed to delete WebAuthn credential {CredentialId}. Status: {StatusCode}", credentialId, response.StatusCode);
                ErrorMessage = Localizer["Error"];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting WebAuthn credential {CredentialId}", credentialId);
            ErrorMessage = Localizer["Error"];
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
