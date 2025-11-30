namespace Vitarish.Web.Clients.Options;

/// <summary>
/// Root configuration options for Vitarish.
/// </summary>
public class VitarishOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string Section = "Vitarish";

    /// <summary>
    /// Identity/authentication options.
    /// </summary>
    public required IdentityOptions Identity { get; set; }
}
