namespace Vitarish.Parties.Domain.Enums;

/// <summary>
/// Method by which a tenant is identified.
/// </summary>
public enum IndicatorMethod
{
    /// <summary>Full custom hostname.</summary>
    Hostname = 1,

    /// <summary>Subdomain under a base domain.</summary>
    Subdomain = 2
}
