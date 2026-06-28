using FluentAssertions;
using Vitarish.Platform.Contracts.Theming;

namespace Platform.Tests.Contracts;

public class ThemeDefaultsTests
{
    [Fact]
    public void All_fallback_values_are_valid_hex_colors()
    {
        var hexPattern = new System.Text.RegularExpressions.Regex(@"^#[0-9A-Fa-f]{6}$");

        var fields = typeof(PlatformThemeDefaults).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToList();

        foreach (var field in fields)
        {
            var value = (string)field.GetValue(null)!;
            hexPattern.IsMatch(value).Should().BeTrue($"{field.Name} should be a valid hex color, got '{value}'");
        }
    }

    [Fact]
    public void Primary_fallback_is_blue()
    {
        PlatformThemeDefaults.FallbackPrimary.Should().Be("#2563EB");
    }

    [Fact]
    public void Dark_mode_has_distinct_values()
    {
        PlatformThemeDefaults.FallbackDarkPrimary.Should().NotBe(PlatformThemeDefaults.FallbackPrimary);
        PlatformThemeDefaults.FallbackDarkBackground.Should().NotBe(PlatformThemeDefaults.FallbackBackground);
    }
}
