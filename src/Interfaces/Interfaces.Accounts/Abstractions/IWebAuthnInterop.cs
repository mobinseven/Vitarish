using System.Text.Json;

namespace Vitarish.Interfaces.Accounts.Abstractions;

/// <summary>
/// Interface for WebAuthn interop operations, providing passkey registration and authentication.
/// </summary>
public interface IWebAuthnInterop
{
    /// <summary>
    /// Checks if WebAuthn is supported in the current browser.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if WebAuthn is supported; otherwise, false.</returns>
    Task<bool> IsWebAuthnSupportedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a platform authenticator (e.g., Touch ID, Windows Hello) is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a platform authenticator is available; otherwise, false.</returns>
    Task<bool> IsPlatformAuthenticatorAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new WebAuthn credential for registration.
    /// </summary>
    /// <param name="options">The credential creation options from the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The attestation response to send to the server.</returns>
    /// <exception cref="WebAuthnNotSupportedException">Thrown when WebAuthn is not supported.</exception>
    /// <exception cref="WebAuthnUserCancelledException">Thrown when the user cancels the operation.</exception>
    /// <exception cref="WebAuthnException">Thrown when an error occurs during credential creation.</exception>
    Task<object> CreateCredentialAsync(JsonElement options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a WebAuthn credential for authentication.
    /// </summary>
    /// <param name="options">The assertion options from the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assertion response to send to the server.</returns>
    /// <exception cref="WebAuthnNotSupportedException">Thrown when WebAuthn is not supported.</exception>
    /// <exception cref="WebAuthnUserCancelledException">Thrown when the user cancels the operation.</exception>
    /// <exception cref="WebAuthnException">Thrown when an error occurs during authentication.</exception>
    Task<object> GetCredentialAsync(JsonElement options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the WebAuthn interop service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when WebAuthn is not supported in the browser.
/// </summary>
public class WebAuthnNotSupportedException : Exception
{
    public WebAuthnNotSupportedException() 
        : base("WebAuthn is not supported in this browser")
    {
    }

    public WebAuthnNotSupportedException(string message) 
        : base(message)
    {
    }

    public WebAuthnNotSupportedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when the user cancels a WebAuthn operation.
/// </summary>
public class WebAuthnUserCancelledException : Exception
{
    public WebAuthnUserCancelledException() 
        : base("The WebAuthn operation was cancelled by the user")
    {
    }

    public WebAuthnUserCancelledException(string message) 
        : base(message)
    {
    }

    public WebAuthnUserCancelledException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a WebAuthn operation fails.
/// </summary>
public class WebAuthnException : Exception
{
    public WebAuthnException() 
        : base("A WebAuthn operation failed")
    {
    }

    public WebAuthnException(string message) 
        : base(message)
    {
    }

    public WebAuthnException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
