using Vitarish.Interface.Identity.Constants;
using Vitarish.Interface.Identity.Contracts;

public static class AccessExtensions
{
    public static string GetClaimAccessValue(this Access access) => $"{access.Read}:{access.Write}:{(access.Admin ? AaasConstants.Admin : string.Empty)}";

    /// <summary>
    /// Creates and populates an <see cref="Access"/> instance from a colon‑delimited string.
    /// Expected layout: "[clientId]:[readRoles]:[writeRoles]:[admin]" where every segment is optional.
    /// • If <c>clientId</c> is not empty ⇒ <see cref="Access.Entrance"/> = <c>true</c>
    /// • If <c>readRoles</c>  is not empty ⇒ <see cref="Access.Read"/>   = segment value
    /// • If <c>writeRoles</c> is not empty ⇒ <see cref="Access.Write"/>  = segment value
    /// • If final segment equals "admin" (case–insensitive) ⇒ <see cref="Access.Admin"/> = <c>true</c>
    /// </summary>
    /// <param name="value">Permission string.</param>
    /// <returns>Configured <see cref="Access"/> object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static Access GenerateAccess(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Access access = new();
        ReadOnlySpan<char> span = value.AsSpan();

        int section = 0;
        int start = 0;

        // Iterate once, treating ':' as delimiters — avoids string.Split allocations.
        for (int i = 0; i <= span.Length && section < 4; i++)
        {
            bool atEnd = i == span.Length;
            if (atEnd || span[i] == ':')
            {
                ReadOnlySpan<char> slice = span[start..i];
                switch (section)
                {
                    case 0:
                        if (!slice.IsEmpty)
                        {
                            access.Entrance = true;
                        }

                        break;
                    case 1:
                        if (!slice.IsEmpty)
                        {
                            access.Read = slice.ToString();
                        }

                        break;
                    case 2:
                        if (!slice.IsEmpty)
                        {
                            access.Write = slice.ToString();
                        }

                        break;
                    case 3:
                        if (slice.Equals("admin".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            access.Admin = true;
                        }

                        break;
                }

                section++;
                start = i + 1; // skip ':'
            }
        }

        return access;
    }
}
