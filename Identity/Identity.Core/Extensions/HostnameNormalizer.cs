using System.Globalization;

namespace Vitarish.Identity.Core.Extensions;

/// <summary>
/// Normalizes hostnames to lowercase punycode format.
/// </summary>
public static class HostnameNormalizer
{
    private static readonly IdnMapping Idn = new() { UseStd3AsciiRules = true };

    /// <summary>
    /// Converts a hostname to canonical form (lowercase, punycode, no trailing dot, no port).
    /// </summary>
    /// <param name="host">The hostname to normalize (may include port).</param>
    /// <returns>The canonical hostname.</returns>
    /// <exception cref="ArgumentException">Thrown when the hostname format is invalid.</exception>
    public static string Canonical(string host)
    {
        try
        {
            // Strip port if present (e.g., "localhost:5001" -> "localhost")
            string hostOnly = host.Split(':', 2)[0];

            string h = hostOnly.Trim().TrimEnd('.').ToLowerInvariant();
            string[] labels = h.Split('.', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = Idn.GetAscii(labels[i]);
            }
            return string.Join('.', labels);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Invalid hostname format: {host}", nameof(host));
        }
    }
}
