namespace Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

using Vitarish.Base.Domain.Primitives;
/// <summary>
/// Represents a frontend authentication client application.
/// </summary>
public class AuthenticationClient : Entity<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationClient"/> class.
    /// Required for EF Core.
    /// </summary>
    private AuthenticationClient() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationClient"/> class.
    /// </summary>
    public AuthenticationClient(string id, string url, string? title, string? description, string? logoUrl, bool autoAccess)
    {
        Id = id;
        Url = url;
        Title = title;
        Description = description;
        LogoUrl = logoUrl;
        AutoAccess = autoAccess;
    }

    /// <summary>Base URL of the client.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>URL of the logo.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Whether the client is allowed immediate access.</summary>
    public bool AutoAccess { get; set; } = true;
}
