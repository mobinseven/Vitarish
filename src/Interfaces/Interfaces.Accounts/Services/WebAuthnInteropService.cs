using Microsoft.JSInterop;
using System.Text.Json;
using Vitarish.Interfaces.Accounts.Abstractions;

namespace Vitarish.Interfaces.Accounts.Services;

/// <summary>
/// Service for WebAuthn interop operations with improved error handling.
/// </summary>
public sealed class WebAuthnInteropService : IWebAuthnInterop, IAsyncDisposable
{
    private const string ModulePath = "./_content/Vitarish.Interfaces.Accounts/Components/Account/WebAuthnInterop.razor.js";
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<WebAuthnInteropService> _logger;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private IJSObjectReference? _module;
    private bool _initialized;
    private bool _disposed;

    public WebAuthnInteropService(IJSRuntime jsRuntime, ILogger<WebAuthnInteropService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized || _disposed)
        {
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized || _disposed)
            {
                return;
            }

            await EnsureModuleAsync(cancellationToken);
            _initialized = true;
            _logger.LogDebug("WebAuthn interop service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize WebAuthn interop service");
            throw new WebAuthnException("Failed to initialize WebAuthn interop service", ex);
        }
        finally
        {
            _ = _initializationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsWebAuthnSupportedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureModuleAsync(cancellationToken);
            
            if (_module is null)
            {
                return false;
            }

            bool isSupported = await _module.InvokeAsync<bool>("isWebAuthnSupported", cancellationToken);
            _logger.LogDebug("WebAuthn support check: {IsSupported}", isSupported);
            return isSupported;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to check WebAuthn support");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking WebAuthn support");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPlatformAuthenticatorAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureModuleAsync(cancellationToken);
            
            if (_module is null)
            {
                return false;
            }

            bool isAvailable = await _module.InvokeAsync<bool>("isPlatformAuthenticatorAvailable", cancellationToken);
            _logger.LogDebug("Platform authenticator availability check: {IsAvailable}", isAvailable);
            return isAvailable;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Failed to check platform authenticator availability");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking platform authenticator availability");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<object> CreateCredentialAsync(JsonElement options, CancellationToken cancellationToken = default)
    {
        await EnsureModuleAsync(cancellationToken);

        if (_module is null)
        {
            throw new WebAuthnNotSupportedException("WebAuthn module could not be loaded");
        }

        try
        {
            _logger.LogDebug("Starting WebAuthn credential creation");
            object credential = await _module.InvokeAsync<object>("createCredential", cancellationToken, options);
            _logger.LogInformation("WebAuthn credential created successfully");
            return credential;
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            _logger.LogInformation(ex, "WebAuthn credential creation cancelled by user");
            throw new WebAuthnUserCancelledException("Credential creation was cancelled by the user", ex);
        }
        catch (JSException ex) when (IsNotSupportedError(ex))
        {
            _logger.LogWarning(ex, "WebAuthn not supported during credential creation");
            throw new WebAuthnNotSupportedException("WebAuthn is not supported in this browser", ex);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "WebAuthn credential creation failed");
            throw new WebAuthnException("Failed to create WebAuthn credential: " + ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during WebAuthn credential creation");
            throw new WebAuthnException("An unexpected error occurred during credential creation", ex);
        }
    }

    /// <inheritdoc />
    public async Task<object> GetCredentialAsync(JsonElement options, CancellationToken cancellationToken = default)
    {
        await EnsureModuleAsync(cancellationToken);

        if (_module is null)
        {
            throw new WebAuthnNotSupportedException("WebAuthn module could not be loaded");
        }

        try
        {
            _logger.LogDebug("Starting WebAuthn authentication");
            object credential = await _module.InvokeAsync<object>("getCredential", cancellationToken, options);
            _logger.LogInformation("WebAuthn authentication successful");
            return credential;
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            _logger.LogInformation(ex, "WebAuthn authentication cancelled by user");
            throw new WebAuthnUserCancelledException("Authentication was cancelled by the user", ex);
        }
        catch (JSException ex) when (IsNotSupportedError(ex))
        {
            _logger.LogWarning(ex, "WebAuthn not supported during authentication");
            throw new WebAuthnNotSupportedException("WebAuthn is not supported in this browser", ex);
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "WebAuthn authentication failed");
            throw new WebAuthnException("Failed to authenticate with WebAuthn: " + ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during WebAuthn authentication");
            throw new WebAuthnException("An unexpected error occurred during authentication", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            if (_module is not null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
        catch (JSDisconnectedException)
        {
            // Expected when the JS runtime is no longer available
            _logger.LogDebug("JS runtime disconnected during WebAuthn service disposal");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during WebAuthn service disposal");
        }
        finally
        {
            _initializationGate.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private async Task EnsureModuleAsync(CancellationToken cancellationToken = default)
    {
        if (_module is not null)
        {
            return;
        }

        try
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load WebAuthn JavaScript module from {ModulePath}", ModulePath);
            throw;
        }
    }

    private static bool IsUserCancellation(JSException ex)
    {
        // Detects user cancellation based on common error message patterns.
        // WebAuthn errors can vary by browser, but cancellation typically includes:
        // - "abort" or "cancel" for explicit user cancellation
        // - "user declined" or "user refused" for rejection
        // - "not allowed" when user doesn't grant permission (but not "not supported")
        string message = ex.Message.ToLowerInvariant();
        return message.Contains("abort") 
            || message.Contains("cancel") 
            || message.Contains("user declined")
            || message.Contains("user refused")
            || (message.Contains("not allowed") && !message.Contains("not supported"));
    }

    private static bool IsNotSupportedError(JSException ex)
    {
        // Detects when WebAuthn is not supported in the browser or authenticator.
        // Checks for "not supported" (browser capability issue) or "not available"
        // (authenticator unavailable), but excludes messages that also contain "cancel"
        // to avoid false positives with cancellation errors.
        string message = ex.Message.ToLowerInvariant();
        return message.Contains("not supported") 
            || (message.Contains("not available") && !message.Contains("cancel"));
    }
}
