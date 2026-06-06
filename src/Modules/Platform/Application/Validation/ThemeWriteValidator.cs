using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Application.Validation;

public static class ThemeWriteValidator
{
    public const string NameRequired = "Theme name is required.";
    public const string ColorRequired = "All palette colors are required.";
    public const string ColorInvalid = "Palette colors must be valid hex values.";

    public static string? Validate(ThemeWriteModel? model)
    {
        if (model is null || string.IsNullOrWhiteSpace(model.Name))
        {
            return NameRequired;
        }

        string? invalidPalette = ValidatePalette(model.LightPalette) ?? ValidatePalette(model.DarkPalette);
        return invalidPalette;
    }

    private static string? ValidatePalette(ThemePalette? palette)
    {
        if (palette is null)
        {
            return ColorRequired;
        }

        if (PaletteValues(palette).Any(string.IsNullOrWhiteSpace))
        {
            return ColorRequired;
        }

        if (PaletteValues(palette).Any(value => !IsHexColor(value)))
        {
            return ColorInvalid;
        }

        return null;
    }

    private static bool IsHexColor(string value)
        => value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

    private static IEnumerable<string> PaletteValues(ThemePalette palette)
    {
        yield return palette.Primary;
        yield return palette.PrimaryContrastText;
        yield return palette.Secondary;
        yield return palette.SecondaryContrastText;
        yield return palette.Background;
        yield return palette.Surface;
        yield return palette.AppbarBackground;
        yield return palette.AppbarText;
        yield return palette.TextPrimary;
        yield return palette.TextSecondary;
        yield return palette.Divider;
    }
}
