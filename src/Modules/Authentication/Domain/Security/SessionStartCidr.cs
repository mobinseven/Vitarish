using System.Net;
using System.Net.Sockets;

namespace Vitarish.Authentication.Domain.Security;

public static class SessionStartCidr
{
    public static IReadOnlySet<string> NormalizeSet(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        HashSet<string> normalizedValues = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            if (!TryParse(value, out string? normalized))
                throw new ArgumentException("Allowed session-start values must be valid CIDRs.", nameof(values));
            if (!normalizedValues.Add(normalized!))
                throw new ArgumentException("Allowed session-start values must not contain duplicate CIDRs.", nameof(values));
        }

        return normalizedValues;
    }

    public static bool TryParse(string? value, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string[] parts = value.Split('/', 2, StringSplitOptions.None);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress? address) || !int.TryParse(parts[1], out int prefix)) return false;
        int width = address.AddressFamily == AddressFamily.InterNetwork ? 32 : address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 0;
        if (width == 0 || prefix < 0 || prefix > width) return false;
        byte[] bytes = address!.GetAddressBytes();
        int wholeBytes = prefix / 8;
        int remainingBits = prefix % 8;
        if (remainingBits != 0)
        {
            bytes[wholeBytes] &= (byte)(0xff << (8 - remainingBits));
            wholeBytes++;
        }
        for (int i = wholeBytes; i < bytes.Length; i++) bytes[i] = 0;
        normalized = $"{new IPAddress(bytes)}/{prefix}";
        return true;
    }

    public static bool Contains(string cidr, IPAddress address)
    {
        if (!TryParse(cidr, out string? normalized) || !TryParseAddress(normalized!, out IPAddress? network, out int prefix)) return false;
        if (network!.AddressFamily != address.AddressFamily) return false;
        byte[] networkBytes = network.GetAddressBytes();
        byte[] addressBytes = address.GetAddressBytes();
        int wholeBytes = prefix / 8;
        int remainingBits = prefix % 8;
        for (int i = 0; i < wholeBytes; i++) if (networkBytes[i] != addressBytes[i]) return false;
        return remainingBits == 0 || (networkBytes[wholeBytes] & (byte)(0xff << (8 - remainingBits))) == (addressBytes[wholeBytes] & (byte)(0xff << (8 - remainingBits)));
    }

    private static bool TryParseAddress(string value, out IPAddress? address, out int prefix)
    {
        address = null;
        prefix = 0;
        string[] parts = value.Split('/', 2);
        return parts.Length == 2 && IPAddress.TryParse(parts[0], out address) && int.TryParse(parts[1], out prefix);
    }
}
