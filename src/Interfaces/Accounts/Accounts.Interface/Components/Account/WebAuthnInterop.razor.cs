using System.Text.Json;
using Vitarish.Accounts.Interface.Abstractions;

namespace Vitarish.Accounts.Interface.Components.Account;

/// <summary>
/// WebAuthn interop component that wraps the WebAuthnInteropService.
/// This component provides a thin layer between Blazor markup and the service.
/// </summary>
public partial class WebAuthnInterop : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IWebAuthnInterop Service { get; set; } = default!;

    /// <summary>
    /// Checks if WebAuthn is supported in the current browser.
    /// </summary>
    public Task<bool> IsWebAuthnSupportedAsync(CancellationToken cancellationToken = default) 
        => Service.IsWebAuthnSupportedAsync(cancellationToken);

    /// <summary>
    /// Checks if a platform authenticator is available.
    /// </summary>
    public Task<bool> IsPlatformAuthenticatorAvailableAsync(CancellationToken cancellationToken = default) 
        => Service.IsPlatformAuthenticatorAvailableAsync(cancellationToken);

    /// <summary>
    /// Creates a new WebAuthn credential.
    /// </summary>
    public Task<object> CreateCredentialAsync(JsonElement options, CancellationToken cancellationToken = default) 
        => Service.CreateCredentialAsync(options, cancellationToken);

    /// <summary>
    /// Gets a WebAuthn credential for authentication.
    /// </summary>
    public Task<object> GetCredentialAsync(JsonElement options, CancellationToken cancellationToken = default) 
        => Service.GetCredentialAsync(options, cancellationToken);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Service.InitializeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Service is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
