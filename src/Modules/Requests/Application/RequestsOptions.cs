namespace Vitarish.Requests.Application;

/// <summary>
/// Options for configuring request identity context resolution.
/// </summary>
public sealed class RequestsOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string Section = "Vitarish:Identity";

    /// <summary>
    /// The authority (issuer) URL for identity validation.
    /// </summary>
    public string? Authority { get; set; }
}
