using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Accounts.Interface.Abstractions;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class WebAuthnLoginForm : IdentityCommonBase, IAsyncDisposable
{
    [Parameter] public string? ReturnUrl { get; set; }

    [Inject] public IWebAuthnInterop WebAuthnService { get; set; } = default!;

    private string Username { get; set; } = string.Empty;
    private string? ErrorMessage { get; set; }
    private bool IsAuthenticating { get; set; }
    private bool IsSupported { get; set; }

    private ILogger<WebAuthnLoginForm>? _logger;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _logger = LoggerFactory.CreateLogger<WebAuthnLoginForm>();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await WebAuthnService.InitializeAsync();
                IsSupported = await WebAuthnService.IsWebAuthnSupportedAsync();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to initialize WebAuthn support check");
                IsSupported = false;
                StateHasChanged();
            }
        }
    }

    private async Task AuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            return;
        }

        IsAuthenticating = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            // Step 1: Begin authentication - get options from server
            HttpClient httpClient = HttpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

            HttpResponseMessage beginResponse = await httpClient.PostAsJsonAsync(
                "/api/webauthn/authenticate/begin",
                new { Username });

            if (!beginResponse.IsSuccessStatusCode)
            {
                ErrorMessage = Localizer["InvalidLoginAttempt"];
                return;
            }

            JsonDocument options = await beginResponse.Content.ReadFromJsonAsync<JsonDocument>()
                ?? throw new InvalidOperationException("Failed to parse authentication options");

            // Step 2: Get credential from authenticator using the service
            object credentialResponse = await WebAuthnService.GetCredentialAsync(options.RootElement);

            // Step 3: Complete authentication - send assertion to server
            HttpResponseMessage completeResponse = await httpClient.PostAsJsonAsync(
                "/api/webauthn/authenticate/complete",
                credentialResponse);

            if (completeResponse.IsSuccessStatusCode)
            {
                // Authentication successful - redirect to return URL
                string redirectUrl = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/";
                NavigationManager.NavigateTo(redirectUrl, forceLoad: true);
            }
            else
            {
                ErrorMessage = Localizer["InvalidLoginAttempt"];
            }
        }
        catch (WebAuthnUserCancelledException ex)
        {
            _logger?.LogInformation(ex, "WebAuthn authentication cancelled by user for username: {Username}", Username);
            ErrorMessage = Localizer["PasskeyAuthenticationCancelled"];
        }
        catch (WebAuthnNotSupportedException ex)
        {
            _logger?.LogWarning(ex, "WebAuthn not supported for username: {Username}", Username);
            ErrorMessage = Localizer["WebAuthnNotSupported"];
        }
        catch (WebAuthnException ex)
        {
            _logger?.LogError(ex, "WebAuthn authentication failed for username: {Username}", Username);
            ErrorMessage = Localizer["InvalidLoginAttempt"];
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during WebAuthn authentication for username: {Username}", Username);
            ErrorMessage = Localizer["InvalidLoginAttempt"];
        }
        finally
        {
            IsAuthenticating = false;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (WebAuthnService is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
