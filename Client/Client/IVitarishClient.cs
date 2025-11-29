namespace Vitarish.Client;

/// <summary>
/// Unified client interface for the Vitarish platform.
/// Provides access to all platform services including Identity, Files, Communications, and AI.
/// </summary>
public interface IVitarishClient
{
    /// <summary>
    /// Gets the identity client for user authentication and management.
    /// </summary>
    IIdentityApi Identity { get; }

    /// <summary>
    /// Gets the files client for file storage operations.
    /// </summary>
    IFilesApi Files { get; }

    /// <summary>
    /// Gets the communications client for messaging services.
    /// </summary>
    ICommunicationsApi Communications { get; }

    /// <summary>
    /// Gets the AI client for AI-powered features.
    /// </summary>
    IAiApi Ai { get; }
}

/// <summary>
/// Interface for identity-related API operations.
/// </summary>
public interface IIdentityApi
{
    /// <summary>
    /// Authenticates a user with the provided credentials.
    /// </summary>
    /// <param name="username">The username or email.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication result.</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user profile.</returns>
    Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    /// <param name="profile">The updated profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for file-related API operations.
/// </summary>
public interface IFilesApi
{
    /// <summary>
    /// Uploads a file.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uploaded file information.</returns>
    Task<FileInfo> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file.
    /// </summary>
    /// <param name="fileId">The file ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file stream.</returns>
    Task<Stream> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file information.
    /// </summary>
    /// <param name="fileId">The file ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file information.</returns>
    Task<FileInfo?> GetInfoAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="fileId">The file ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(Guid fileId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for communications-related API operations.
/// </summary>
public interface ICommunicationsApi
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="to">The recipient email address.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="body">The message body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS message.
    /// </summary>
    /// <param name="to">The recipient phone number.</param>
    /// <param name="message">The message text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The send result.</returns>
    Task<SmsSendResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for AI-related API operations.
/// </summary>
public interface IAiApi
{
    /// <summary>
    /// Gets a chat completion from the AI.
    /// </summary>
    /// <param name="prompt">The user prompt.</param>
    /// <param name="systemInstructions">Optional system instructions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI response.</returns>
    Task<string> ChatAsync(string prompt, string? systemInstructions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transcribes audio to text.
    /// </summary>
    /// <param name="audioData">The audio data.</param>
    /// <param name="language">Optional language hint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text.</returns>
    Task<string> TranscribeAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public record AuthenticationResult
{
    /// <summary>
    /// Gets whether authentication was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the access token if authentication was successful.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token if available.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets the error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// User profile information.
/// </summary>
public record UserProfile
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; init; }
}

/// <summary>
/// File information.
/// </summary>
public record FileInfo
{
    /// <summary>
    /// Gets or sets the file ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Gets or sets the URL where the file can be accessed.
    /// </summary>
    public string? Url { get; init; }
}

/// <summary>
/// Result of sending an SMS message.
/// </summary>
public record SmsSendResult
{
    /// <summary>
    /// Gets whether the SMS was sent successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the message ID.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Gets the error message if sending failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
