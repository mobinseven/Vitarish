using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Domain.Models;

namespace Vitarish.Platform.Infrastructure.Services;

public static class DefaultPlatformTheme
{
    public static readonly Guid Id = Guid.Parse("3af03f4e-e53a-4f63-97cc-820c5ea52d85");
    public const string Name = "Default";

    public static Theme Create(DateTimeOffset now) => new()
    {
        Id = Id,
        Name = Name,
        CreatedAt = now,
        UpdatedAt = now,
        LightPalette = new ThemePalette
        {
            Primary = "#2563EB",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#0EA5E9",
            SecondaryContrastText = "#001018",
            Background = "#F1F1F1",
            Surface = "#F1F1F1",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0F172A",
            TextPrimary = "#0F172A",
            TextSecondary = "#334155",
            Divider = "#E2E8F0"
        },
        DarkPalette = new ThemePalette
        {
            Primary = "#60A5FA",
            PrimaryContrastText = "#07101F",
            Secondary = "#38BDF8",
            SecondaryContrastText = "#041018",
            Background = "#323232",
            Surface = "#323232",
            AppbarBackground = "#434343",
            AppbarText = "#E2E8F0",
            TextPrimary = "#E2E8F0",
            TextSecondary = "#94A3B8",
            Divider = "#233044"
        }
    };
}
