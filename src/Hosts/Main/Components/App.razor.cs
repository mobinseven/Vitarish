namespace Vitarish.Main.Components;

public partial class App
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;
    private IComponentRenderMode? RenderModeForPage => HttpContext.Request.Path.StartsWithSegments("/Account")
        ? null : InteractiveServer;
    private static readonly string[] FontFaminly = ["Ahang", "SegoeUI", "system-ui", "Segoe UI", "Roboto", "Arial", "sans-serif"];
    private readonly MudTheme theme = new()
    {
        LayoutProperties = new LayoutProperties
        {
            // ChatGPT surfaces tend to read better with slightly softer corners than 8px.
            DefaultBorderRadius = "12px"
        },

        PaletteLight = new PaletteLight
        {
            // Brand / accents
            Primary = "#0B57D0",
            PrimaryContrastText = "#FFFFFF",
            PrimaryLighten = "#3B82F6",
            PrimaryDarken = "#083A8B",

            Secondary = "#5B6B7A",
            SecondaryContrastText = "#FFFFFF",
            SecondaryLighten = "#7B8794",
            SecondaryDarken = "#3F4A55",

            // Optional “support” accent (nice for highlights/badges that shouldn’t be Primary)
            Tertiary = "#10A37F",
            TertiaryContrastText = "#FFFFFF",

            // Surfaces
            Background = "#F7F7F8",
            Surface = "#FFFFFF",

            // App chrome
            AppbarBackground = "rgba(255,255,255,0.72)",
            AppbarText = "#111827",

            DrawerBackground = "#FFFFFF",
            DrawerText = "#111827",
            DrawerIcon = "#6B7280",

            // Text
            TextPrimary = "#111827",
            TextSecondary = "#6B7280",
            TextDisabled = "rgba(17,24,39,0.38)",

            // Lines / dividers
            Divider = "rgba(17,24,39,0.12)",
            LinesDefault = "rgba(17,24,39,0.12)",
            LinesInputs = "rgba(17,24,39,0.22)",

            // Actions
            ActionDefault = "#374151",
            ActionDisabled = "rgba(17,24,39,0.34)",
            ActionDisabledBackground = "rgba(17,24,39,0.06)",

            // Feedback colors
            Error = "#DC2626",
            ErrorContrastText = "#FFFFFF",
            Warning = "#D97706",
            WarningContrastText = "#111827",
            Success = "#16A34A",
            SuccessContrastText = "#FFFFFF",
            Info = "#2563EB",
            InfoContrastText = "#FFFFFF",

            // Tables (kept very subtle to match “card” feel)
            TableLines = "rgba(17,24,39,0.08)",
            TableStriped = "rgba(17,24,39,0.03)",
            TableHover = "rgba(11,87,208,0.06)",

            // Overlays
            OverlayLight = "rgba(255,255,255,0.72)",
            OverlayDark = "rgba(17,24,39,0.72)",
        },

        PaletteDark = new PaletteDark
        {
            // Brand / accents (slightly brighter for dark backgrounds)
            Primary = "#3B82F6",
            PrimaryContrastText = "#0B0F19",
            PrimaryLighten = "#60A5FA",
            PrimaryDarken = "#1D4ED8",

            Secondary = "#94A3B8",
            SecondaryContrastText = "#0B0F19",
            SecondaryLighten = "#CBD5E1",
            SecondaryDarken = "#64748B",

            Tertiary = "#10A37F",
            TertiaryContrastText = "#04110C",

            // Surfaces
            Background = "#0B0F19",
            Surface = "#111827",

            // App chrome
            AppbarBackground = "rgba(17,24,39,0.72)",
            AppbarText = "#F9FAFB",

            DrawerBackground = "#0B0F19",
            DrawerText = "#F9FAFB",
            DrawerIcon = "#9CA3AF",

            // Text
            TextPrimary = "#F9FAFB",
            TextSecondary = "#9CA3AF",
            TextDisabled = "rgba(249,250,251,0.38)",

            // Lines / dividers
            Divider = "rgba(249,250,251,0.12)",
            LinesDefault = "rgba(249,250,251,0.12)",
            LinesInputs = "rgba(249,250,251,0.18)",

            // Actions
            ActionDefault = "#E5E7EB",
            ActionDisabled = "rgba(249,250,251,0.30)",
            ActionDisabledBackground = "rgba(249,250,251,0.06)",

            // Feedback colors (tuned for dark)
            Error = "#F87171",
            ErrorContrastText = "#0B0F19",
            Warning = "#FDBA74",
            WarningContrastText = "#0B0F19",
            Success = "#4ADE80",
            SuccessContrastText = "#0B0F19",
            Info = "#60A5FA",
            InfoContrastText = "#0B0F19",

            // Tables
            TableLines = "rgba(249,250,251,0.10)",
            TableStriped = "rgba(249,250,251,0.03)",
            TableHover = "rgba(59,130,246,0.12)",

            // Overlays
            OverlayLight = "rgba(249,250,251,0.72)",
            OverlayDark = "rgba(0,0,0,0.72)",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontFaminly,
                FontSize = "16px",
                FontWeight = "400",
                LineHeight = "1.5"
            },

            // A “developer UI” hierarchy: tighter headings, no shouty buttons.
            H1 = new H1Typography { FontFamily = FontFaminly, FontSize = "2.125rem", FontWeight = "700", LineHeight = "1.15", LetterSpacing = "-0.02em" },
            H2 = new H2Typography { FontFamily = FontFaminly, FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.015em" },
            H3 = new H3Typography { FontFamily = FontFaminly, FontSize = "1.5rem", FontWeight = "650", LineHeight = "1.25" },
            H4 = new H4Typography { FontFamily = FontFaminly, FontSize = "1.25rem", FontWeight = "650", LineHeight = "1.3" },
            H5 = new H5Typography { FontFamily = FontFaminly, FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.35" },
            H6 = new H6Typography { FontFamily = FontFaminly, FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" },

            Body1 = new Body1Typography { FontFamily = FontFaminly, FontSize = "1rem", FontWeight = "400", LineHeight = "1.6" },
            Body2 = new Body2Typography { FontFamily = FontFaminly, FontSize = "0.9375rem", FontWeight = "400", LineHeight = "1.55" },

            Subtitle1 = new Subtitle1Typography { FontFamily = FontFaminly, FontSize = "0.9375rem", FontWeight = "600", LineHeight = "1.45" },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontFaminly, FontSize = "0.875rem", FontWeight = "600", LineHeight = "1.45" },

            Button = new ButtonTypography
            {
                FontFamily = FontFaminly,
                FontSize = "0.875rem",
                FontWeight = "600",
                LineHeight = "1.2",
                LetterSpacing = "0.01em",
                TextTransform = "none"
            },

            Caption = new CaptionTypography { FontFamily = FontFaminly, FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.35" }
        }
    };
}

