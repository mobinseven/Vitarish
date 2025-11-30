using System.Security.Cryptography;
using System.Text;
using Vitarish.Identity.Core.Domain.Enums;

namespace Vitarish.Identity.Core.Extensions;

/// <summary>
/// Generates deterministic authentication client identifiers for tenant indicators.
/// </summary>
public static class AuthClientIdGenerator
{
    // RFC 4648 lowercase Base32 alphabet, intentionally omitting padding ('=') for deterministic ID generation.
    // This is a deliberate deviation from the full RFC 4648 spec, as padding is not required for this use case.
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

    /// <summary>
    /// Generates a deterministic client ID from tenant indicator data.
    /// Format: ac:{base32(sha256(tenantId|method|value))[:32]}
    /// </summary>
    public static string Generate(Guid tenantId, IndicatorMethod method, string value)
    {
        string input = $"{tenantId}|{(int)method}|{value}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        string base32 = ToBase32(hash);
        return $"ac:{base32[..32]}";
    }

    private static string ToBase32(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new(((data.Length * 8) + 4) / 5);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                _ = result.Append(Alphabet[(buffer >> (bitsLeft - 5)) & 0x1F]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            _ = result.Append(Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return result.ToString();
    }
}
