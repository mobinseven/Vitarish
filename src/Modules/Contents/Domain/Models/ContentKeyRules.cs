namespace Vitarish.Contents.Domain.Models;

public static class ContentKeyRules
{
    public static bool IsSlug(string? value)
    {
        if (string.IsNullOrEmpty(value) || !IsAsciiLowerOrDigit(value[0]))
        {
            return false;
        }

        bool previousWasSeparator = false;
        foreach (char character in value)
        {
            if (character == '-')
            {
                if (previousWasSeparator)
                {
                    return false;
                }

                previousWasSeparator = true;
                continue;
            }

            if (!IsAsciiLowerOrDigit(character))
            {
                return false;
            }

            previousWasSeparator = false;
        }

        return !previousWasSeparator;
    }

    public static bool IsCamelCase(string? value)
    {
        if (string.IsNullOrEmpty(value) || value[0] is < 'a' or > 'z')
        {
            return false;
        }

        return value.All(character => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');
    }

    private static bool IsAsciiLowerOrDigit(char character) => character is >= 'a' and <= 'z' or >= '0' and <= '9';
}
