using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Accounts.Interface.Components.Base;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class WebAuthnLoginForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }

    private string Username { get; set; } = string.Empty;
    private string? ErrorMessage { get; set; }
    private bool IsAuthenticating { get; set; }
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
                StateHasChanged();
            }
            catch
            {
                IsSupported = false;
                StateHasChanged();
            }
        }
    }

    private async Task AuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || _jsModule is null)
        {
            return;
        }

        IsAuthenticating = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            // Step 1: Begin authentication - get options from server
            using HttpClient httpClient = new();
            httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);

            HttpResponseMessage beginResponse = await httpClient.PostAsJsonAsync(
                "/api/webauthn/authenticate/begin",
                new { Username });

            if (!beginResponse.IsSuccessStatusCode)
            {
                string error = await beginResponse.Content.ReadAsStringAsync();
                ErrorMessage = Localizer["InvalidLoginAttempt"];
                return;
            }

            JsonDocument options = await beginResponse.Content.ReadFromJsonAsync<JsonDocument>()
                ?? throw new InvalidOperationException("Failed to parse authentication options");

            // Step 2: Get credential from authenticator
            object credentialResponse = await _jsModule.InvokeAsync<object>(
                "getCredential",
                options.RootElement);

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
        catch (JSException)
        {
            ErrorMessage = Localizer["PasskeyAuthenticationCancelled"];
        }
        catch (Exception)
        {
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
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
