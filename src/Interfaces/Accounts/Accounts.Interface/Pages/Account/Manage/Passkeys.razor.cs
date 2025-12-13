using System.Net.Http.Json;
using System.Text.Json;

namespace Vitarish.Accounts.Interface.Pages.Account.Manage;

public partial class Passkeys : IAsyncDisposable
{
    private List<PasskeyCredentialDto> Credentials { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private bool IsRegistering { get; set; }
    private bool IsDeletingCredential { get; set; }
    private bool IsSupported { get; set; }

    private IJSObjectReference? _jsModule;
    private ILogger<Passkeys>? _logger;

    protected override void OnInitialized()
    {
        _logger = LoggerFactory.CreateLogger<Passkeys>();
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

    private async Task LoadCredentialsAsync()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
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
                _logger?.LogWarning("Failed to load WebAuthn credentials. Status: {StatusCode}", response.StatusCode);
                Snackbar.Add(Localizer["Error"], Severity.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading WebAuthn credentials");
            Snackbar.Add(Localizer["Error"], Severity.Error);
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
        StateHasChanged();

        try
        {
            using HttpClient httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

            // Step 1: Begin registration - get options from server
            HttpResponseMessage beginResponse = await httpClient.PostAsync("/api/webauthn/register/begin", null);

            if (!beginResponse.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Failed to begin WebAuthn registration. Status: {StatusCode}", beginResponse.StatusCode);
                Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
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
                Snackbar.Add(Localizer["PasskeyRegistered"], Severity.Success);
                await LoadCredentialsAsync();
            }
            else
            {
                _logger?.LogWarning("Failed to complete WebAuthn registration. Status: {StatusCode}", completeResponse.StatusCode);
                Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
            }
        }
        catch (JSException ex)
        {
            _logger?.LogInformation(ex, "WebAuthn registration cancelled by user");
            Snackbar.Add(Localizer["PasskeyRegistrationCancelled"], Severity.Warning);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during WebAuthn registration");
            Snackbar.Add(Localizer["PasskeyRegistrationFailed"], Severity.Error);
        }
        finally
        {
            IsRegistering = false;
            StateHasChanged();
        }
    }

    private async Task DeletePasskeyAsync(Guid credentialId)
    {
        IsDeletingCredential = true;
        StateHasChanged();

        try
        {
            using HttpClient httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

            HttpResponseMessage response = await httpClient.DeleteAsync($"/api/webauthn/credentials/{credentialId}");

            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add(Localizer["PasskeyDeleted"], Severity.Success);
                await LoadCredentialsAsync();
            }
            else
            {
                _logger?.LogWarning("Failed to delete WebAuthn credential {CredentialId}. Status: {StatusCode}", credentialId, response.StatusCode);
                Snackbar.Add(Localizer["Error"], Severity.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting WebAuthn credential {CredentialId}", credentialId);
            Snackbar.Add(Localizer["Error"], Severity.Error);
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
