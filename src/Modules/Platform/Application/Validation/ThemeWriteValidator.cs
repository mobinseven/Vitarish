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
    {
        if (value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (int i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> PaletteValues(ThemePalette palette)
    {
        yield return palette.Primary;
        yield return palette.PrimaryContrastText;
        yield return palette.Secondary;
        yield return palette.SecondaryContrastText;
        yield return palette.Tertiary;
        yield return palette.TertiaryContrastText;
        yield return palette.Info;
        yield return palette.InfoContrastText;
        yield return palette.Success;
        yield return palette.SuccessContrastText;
        yield return palette.Warning;
        yield return palette.WarningContrastText;
        yield return palette.Error;
        yield return palette.ErrorContrastText;
        yield return palette.Background;
        yield return palette.Surface;
        yield return palette.AppbarBackground;
        yield return palette.AppbarText;
        yield return palette.TextPrimary;
        yield return palette.TextSecondary;
        yield return palette.Divider;
    }
}
