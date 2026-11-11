using System.Text;

namespace Vitarish.Parties.Domain.Addresses;

/// <summary>Why a requested subdomain cannot become a tenant address.</summary>
public enum TenantSubdomainRejection
{
    None = 0,
    /// <summary>Not 3–30 characters of <c>a-z</c>, <c>0-9</c>, and inner single hyphens.</summary>
    Malformed = 1,
    /// <summary>Kept for the platform's own hosts and well-known service names.</summary>
    Reserved = 2
}

/// <summary>
/// The leftmost DNS label of a tenant's own address, <c>&lt;subdomain&gt;.&lt;base domain&gt;</c>.
/// </summary>
public sealed record TenantSubdomain
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 30;

    private TenantSubdomain(string value) => Value = value;

    /// <summary>Lowercase label, without the base domain.</summary>
    public string Value { get; }

    /// <summary>
    /// Trims and lowercases <paramref name="candidate"/>, then applies the address rules.
    /// </summary>
    public static TenantSubdomainRejection TryCreate(string? candidate, out TenantSubdomain? subdomain)
    {
        subdomain = null;
        string value = (candidate ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsWellFormed(value))
            return TenantSubdomainRejection.Malformed;
        if (ReservedTenantSubdomains.Contains(value))
            return TenantSubdomainRejection.Reserved;

        subdomain = new TenantSubdomain(value);
        return TenantSubdomainRejection.None;
    }

    /// <summary>
    /// A well-formed, unreserved address for a clinic that did not choose one: the Latin letters
    /// and digits of <paramref name="name"/> followed by a short code taken from
    /// <paramref name="entropy"/>. The same inputs always give the same address, so a retried
    /// creation finds the clinic it already made.
    /// </summary>
    public static TenantSubdomain Generate(string name, ReadOnlySpan<byte> entropy)
    {
        const string codeAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        const int codeLength = 5;
        const string fallbackStem = "clinic";
        const int stemMaximumLength = MaximumLength - codeLength - 1;

        StringBuilder stem = new();
        foreach (char character in name.ToLowerInvariant())
        {
            if (character is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                stem.Append(character);
            else if (stem.Length > 0 && stem[^1] != '-')
                stem.Append('-');
        }
        string label = stem.ToString().Trim('-');
        if (label.Length > stemMaximumLength)
            label = label[..stemMaximumLength].TrimEnd('-');
        if (label.Length == 0)
            label = fallbackStem;

        char[] code = new char[codeLength];
        for (int index = 0; index < codeLength; index++)
            code[index] = codeAlphabet[entropy[index] % codeAlphabet.Length];
        return new TenantSubdomain($"{label}-{new string(code)}");
    }

    /// <summary>The full hostname under <paramref name="baseDomain"/>.</summary>
    public string ToHostname(string baseDomain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDomain);
        return $"{Value}.{baseDomain.Trim().TrimEnd('.').ToLowerInvariant()}";
    }

    public override string ToString() => Value;

    // Hyphens may not lead, trail, or repeat; a repeated hyphen would also allow the
    // "xn--" punycode prefix, which renders as a different name in browsers.
    private static bool IsWellFormed(string value) =>
        value.Length is >= MinimumLength and <= MaximumLength
        && value.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
        && value[0] != '-'
        && value[^1] != '-'
        && !value.Contains("--", StringComparison.Ordinal);
}
