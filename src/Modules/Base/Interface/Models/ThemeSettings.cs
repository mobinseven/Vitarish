using System.Text.Json.Serialization;
using Vitarish.Modules.Base.Interface.Enums;

namespace Vitarish.Modules.Base.Interface.Models;

public sealed record ThemeSettings(
    ThemePreference Theme = ThemePreference.Auto,
    TypographySize Typography = TypographySize.Normal,
    ThemeSurfaceType Surface = ThemeSurfaceType.Translucent,
    bool? LastKnownIsDarkMode = null)
{
    public static ThemeSettings Default => new();

    public string ToJson(JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(this, options ?? JsonOptions);

    public static ThemeSettings? FromJson(string? json, JsonSerializerOptions? options = null)
        => string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<ThemeSettings>(json, options ?? JsonOptions);

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
