namespace Vorn.Aaas.Client.Api.Contracts;

/// <summary>
/// Data transfer object that represents API client credentials and metadata.
/// </summary>
public class ApiClientDto
{
    /// <summary>
    /// The client identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The client secret.
    /// </summary>
    public required string Secret { get; set; }

    /// <summary>
    /// Space separated scopes requested by the client.
    /// </summary>
    public required string Scopes { get; set; }

    /// <summary>
    /// The host URI for the API the client targets.
    /// </summary>
    public required string HostUri { get; set; }
}
