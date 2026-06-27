namespace Vitarish.Modules.Base.Interface.Common;

public partial class ColorValueChip : ComponentBase
{
    [Parameter] public string? Value { get; set; }
    [Parameter] public string? DisplayText { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string Class { get; set; } = string.Empty;

    private string CssClass => string.IsNullOrWhiteSpace(Class)
        ? "theme-color-chip"
        : $"theme-color-chip {Class}";

    private Variant ChipVariant => string.IsNullOrWhiteSpace(Value) ? Variant.Outlined : Variant.Filled;

    private string ChipText => string.IsNullOrWhiteSpace(Value)
        ? Placeholder ?? string.Empty
        : DisplayText ?? Value!;

    private string ChipStyle => string.IsNullOrWhiteSpace(Value)
        ? string.Empty
        : $"background-color: {Value}; color: {GetForegroundColor(Value)}; border-color: {Value};";

    private static string GetForegroundColor(string color) => TryParseRgb(color, out int r, out int g, out int b)
        ? GetForegroundFromRgb(r, g, b)
        : "rgba(0, 0, 0, 0.87)";

    private static string GetForegroundFromRgb(int r, int g, int b)
    {
        double luminance = ((0.299 * r) + (0.587 * g) + (0.114 * b)) / 255d;
        return luminance > 0.5 ? "rgba(0, 0, 0, 0.87)" : "rgba(255, 255, 255, 0.87)";
    }

    private static bool TryParseRgb(string color, out int r, out int g, out int b)
    {
        r = g = b = 0;

        if (string.IsNullOrWhiteSpace(color))
        {
            return false;
        }

        string trimmed = color.Trim();

        if (trimmed.StartsWith('#'))
        {
            string hex = trimmed[1..];
            switch (hex.Length)
            {
                case 3:
                    r = Convert.ToInt32(new string(hex[0], 2), 16);
                    g = Convert.ToInt32(new string(hex[1], 2), 16);
                    b = Convert.ToInt32(new string(hex[2], 2), 16);
                    return true;
                case 6:
                    r = Convert.ToInt32(hex[..2], 16);
                    g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return true;
                case 8:
                    r = Convert.ToInt32(hex.Substring(2, 2), 16);
                    g = Convert.ToInt32(hex.Substring(4, 2), 16);
                    b = Convert.ToInt32(hex.Substring(6, 2), 16);
                    return true;
            }

            return false;
        }

        if (trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            int start = trimmed.IndexOf('(');
            int end = trimmed.IndexOf(')');
            if (start >= 0 && end > start)
            {
                string[] parts = trimmed.Substring(start + 1, end - start - 1)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length >= 3 && TryParseComponent(parts[0], out r) && TryParseComponent(parts[1], out g) && TryParseComponent(parts[2], out b))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryParseComponent(string component, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(component))
        {
            return false;
        }

        string trimmed = component.Trim();

        if (trimmed.EndsWith('%'))
        {
            if (!double.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
            {
                return false;
            }

            value = (int)Math.Round(percent * 2.55);
            value = Math.Clamp(value, 0, 255);
            return true;
        }

        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric))
        {
            return false;
        }

        if (numeric is >= 0 and <= 1)
        {
            numeric *= 255d;
        }

        value = (int)Math.Round(numeric);
        value = Math.Clamp(value, 0, 255);
        return true;
    }
}
