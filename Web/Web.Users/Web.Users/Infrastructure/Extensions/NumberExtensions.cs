namespace Vitarish.Web.Users.Infrastructure.Extensions;

internal static class NumberExtensions
{
    public static string ConvertToEnglishNumerals(this string input)
    {
        string[] persianNumerals = { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
        string[] arabicIndicNumerals = { "\u0660", "\u0661", "\u0662", "\u0663", "\u0664", "\u0665", "\u0666", "\u0667", "\u0668", "\u0669" };
        string[] englishNumerals = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        for(int i = 0; i < 10; i++)
        {
            input = input.Replace(persianNumerals[i], englishNumerals[i]);
            input = input.Replace(arabicIndicNumerals[i], englishNumerals[i]);
        }
        return input;
    }
}
