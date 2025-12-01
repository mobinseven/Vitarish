namespace Vitarish.Identity.Core.Parties.Domain.Enums;

/// <summary>
/// Method used to verify domain ownership.
/// </summary>
public enum IndicatorVerificationMethod
{
    /// <summary>Manual verification by admin.</summary>
    Manual = 0,

    /// <summary>DNS TXT record verification.</summary>
    DnsTxt = 1
}
