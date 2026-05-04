namespace Vitarish.Calendar.Infrastructure.Options;

public class RadicaleOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public RadicaleAuthScheme AuthScheme { get; set; } = RadicaleAuthScheme.None;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TlsSkipVerify { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public string PrincipalPrefix { get; set; } = "/";
}

public enum RadicaleAuthScheme
{
    None,
    Basic,
    BearerToken
}
