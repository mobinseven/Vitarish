using System.Globalization;

namespace Vitarish.Interfaces.Base.Extensions;

public static class NumberExtensions
{
    public static string ToCompactString(this long value)
        => ((decimal)value).ToCompactString();

    public static string ToCompactString(this decimal value)
    {
        decimal absolute = Math.Abs(value);
        if (absolute >= 1_000_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#}M", value / 1_000_000m);
        }

        if (absolute >= 1_000m)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#}K", value / 1_000m);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string ConvertToEnglishNumerals(this string input)
    {
        string[] persianNumerals = { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
        string[] arabicIndicNumerals = { "\u0660", "\u0661", "\u0662", "\u0663", "\u0664", "\u0665", "\u0666", "\u0667", "\u0668", "\u0669" };
        string[] englishNumerals = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        for (int i = 0; i < 10; i++)
        {
            input = input.Replace(persianNumerals[i], englishNumerals[i]);
            input = input.Replace(arabicIndicNumerals[i], englishNumerals[i]);
        }
        return input;
    }
}
