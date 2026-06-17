using System.Globalization;
using System.Net;
using Vitarish.Interfaces.Base.Enums;

namespace Vitarish.Interfaces.Base.Extensions;

public static class PaletteCss
{
    public const string DefaultStyleElementId = "vitarish-palette-css";

    public const string PaletteAutoClass = "palette-auto";
    public const string PaletteLightClass = "palette-light";
    public const string PaletteDarkClass = "palette-dark";

    public static string GetPaletteRootCss(PaletteOptions? options = null) =>
        BuildPaletteCss(options ?? PaletteOptions.Default);

    public static string GetStyleTag(
        string styleElementId = DefaultStyleElementId,
        string? nonce = null,
        PaletteOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(styleElementId))
            throw new ArgumentException("Style element id cannot be empty.", nameof(styleElementId));

        string encodedId = WebUtility.HtmlEncode(styleElementId);
        string nonceAttribute = string.IsNullOrWhiteSpace(nonce)
            ? string.Empty
            : $" nonce=\"{WebUtility.HtmlEncode(nonce)}\"";

        return $"""
        <style id="{encodedId}"{nonceAttribute}>
        {GetPaletteRootCss(options)}
        </style>
        """;
    }

    public static string GetCssClass(Palettes palette) => GetDefinition(palette).CssClass;

    public static string? GetCssClass(Palettes? palette) =>
        palette is null ? null : GetCssClass(palette.Value);

    public static string GetDataPaletteValue(Palettes palette) =>
        GetDefinition(palette).Name;

    public static string? GetDataPaletteValue(Palettes? palette) =>
        palette is null ? null : GetDataPaletteValue(palette.Value);

    public static string? GetToneCssClass(PaletteTone tone, string componentClass = "palette-button")
    {
        if (tone == PaletteTone.Auto)
            return null;

        if (!Enum.IsDefined(tone))
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unsupported palette tone.");

        if (string.IsNullOrWhiteSpace(componentClass))
            throw new ArgumentException("Component class cannot be empty.", nameof(componentClass));

        return $"{componentClass}--{GetToneValue(tone)}";
    }

    public static string? GetToneValue(PaletteTone tone)
    {
        if (tone == PaletteTone.Auto)
            return null;

        if (!Enum.IsDefined(tone))
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unsupported palette tone.");

        return ((int)tone).ToString(CultureInfo.InvariantCulture);
    }


    public static PaletteTone GetRelativeTone(
        PaletteTone tone,
        int offset,
        PaletteTone defaultTone = PaletteTone.Tone700)
    {
        PaletteTone resolvedTone = tone == PaletteTone.Auto ? defaultTone : tone;

        if (!Enum.IsDefined(resolvedTone) || resolvedTone == PaletteTone.Auto)
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unsupported palette tone.");

        if (!Enum.IsDefined(defaultTone) || defaultTone == PaletteTone.Auto)
            throw new ArgumentOutOfRangeException(nameof(defaultTone), defaultTone, "Default tone must be a concrete palette tone.");

        int index = Array.IndexOf(ToneOrder, resolvedTone);

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "Unsupported palette tone.");

        int resolvedIndex = PositiveModulo(index + offset, ToneOrder.Length);
        return ToneOrder[resolvedIndex];
    }

    private static int PositiveModulo(int value, int divisor) =>
        ((value % divisor) + divisor) % divisor;

    public static string GetCssClass(Palettes palette, PaletteMode mode)
    {
        string paletteClass = GetCssClass(palette);

        return mode switch
        {
            PaletteMode.Light => $"{paletteClass} {PaletteLightClass}",
            PaletteMode.Dark => $"{paletteClass} {PaletteDarkClass}",
            _ => $"{paletteClass} {PaletteAutoClass}"
        };
    }

    public static string GetHeaderImageUrl(
        Palettes palette,
        PaletteMode mode,
        PaletteOptions? options = null)
    {
        PaletteDefinition definition = GetDefinition(palette);
        PaletteOptions resolvedOptions = options ?? PaletteOptions.Default;

        return BuildHeaderImageUrl(
            definition,
            mode == PaletteMode.Dark ? PaletteMode.Dark : PaletteMode.Light,
            resolvedOptions
        );
    }

    public static string GetCssVariables(Palettes palette) =>
        GetCssVariables(GetDefinition(palette), PaletteMode.Light, PaletteOptions.Default);

    public static string GetCssVariables(Palettes palette, bool darkMode) =>
        GetCssVariables(
            GetDefinition(palette),
            darkMode ? PaletteMode.Dark : PaletteMode.Light,
            PaletteOptions.Default
        );

    public static string GetCssVariables(Palettes palette, PaletteMode mode) =>
        GetCssVariables(
            GetDefinition(palette),
            mode == PaletteMode.Dark ? PaletteMode.Dark : PaletteMode.Light,
            PaletteOptions.Default
        );

    private static string BuildPaletteCss(PaletteOptions options)
    {
        string[] sections =
        [
            """
            /*
             * Vitarish palette variables.
             *
             * Palette classes:
             *   .palette-green
             *   .palette-pink
             *   .palette-yellow
             *   .palette-blue
             *   .palette-purple
             *   .palette-lagoon
             *   .palette-sunset
             *
             * Mode classes:
             *   .palette-auto   uses prefers-color-scheme
             *   .palette-light  forces light palette
             *   .palette-dark   forces dark palette
             *
             * Also supported:
             *   [data-palette="green|pink|yellow|blue|purple|lagoon|sunset"]
             *   [data-theme="light|dark"]
             *   .light
             *   .dark
             *
             * Header image default pattern:
             *   /bg/headers/{palette}-{mode}.webp
             */
            """,

            BuildBaseLightBlocks(options),
            BuildAutoDarkBlocks(options),
            BuildExplicitDarkBlocks(options),
            BuildExplicitLightBlocks(options),
            BuildBodyBackgroundBlock(options)
        ];

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            sections.Where(section => !string.IsNullOrWhiteSpace(section))
        );
    }

    private static string BuildBaseLightBlocks(PaletteOptions options) =>
        string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            Definitions.Select(definition =>
                BuildPaletteRootBlock(GetBaseLightSelectors(definition), definition, PaletteMode.Light, options)
            )
        );

    private static string BuildAutoDarkBlocks(PaletteOptions options)
    {
        string darkBlocks = string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            Definitions.Select(definition =>
                BuildPaletteRootBlock(GetAutoDarkSelectors(definition), definition, PaletteMode.Dark, options)
            )
        );

        return $$"""
        @media (prefers-color-scheme: dark) {
        {{Indent(darkBlocks, 4)}}
        }
        """;
    }

    private static string BuildExplicitDarkBlocks(PaletteOptions options) =>
        string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            Definitions.Select(definition =>
                BuildPaletteRootBlock(GetExplicitDarkSelectors(definition), definition, PaletteMode.Dark, options)
            )
        );

    private static string BuildExplicitLightBlocks(PaletteOptions options) =>
        string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            Definitions.Select(definition =>
                BuildPaletteRootBlock(GetExplicitLightSelectors(definition), definition, PaletteMode.Light, options)
            )
        );

    private static string BuildPaletteRootBlock(
        string selector,
        PaletteDefinition definition,
        PaletteMode mode,
        PaletteOptions options) => $$"""
        {{selector}} {
        {{Indent(GetCssVariables(definition, mode, options), 4)}}
        }
        """;

    private static string BuildBodyBackgroundBlock(PaletteOptions options)
    {
        if (!options.IncludeBodyBackgroundCss)
            return string.Empty;

        string bodyBackgroundStyleBlock = BuildBodyBackgroundStyleBlock(options);

        if (options.BodyPalette is not { } bodyPalette)
            return bodyBackgroundStyleBlock;

        PaletteDefinition bodyDefinition = GetDefinition(bodyPalette);

        string bodyPaletteBlocks = options.BodyPaletteMode switch
        {
            PaletteMode.Light => BuildBodyPaletteVariablesBlock(
                GetBodySelector(options),
                bodyDefinition,
                PaletteMode.Light,
                options
            ),

            PaletteMode.Dark => BuildBodyPaletteVariablesBlock(
                GetBodySelector(options),
                bodyDefinition,
                PaletteMode.Dark,
                options
            ),

            _ => BuildAutoBodyPaletteVariablesBlocks(bodyDefinition, options)
        };

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            new[] { bodyPaletteBlocks, bodyBackgroundStyleBlock }
                .Where(section => !string.IsNullOrWhiteSpace(section))
        );
    }

    private static string BuildBodyBackgroundStyleBlock(PaletteOptions options) => $$"""
    {{options.BodySelector}} {
        background-color: var(--palette-body-bg);
        background-image: var(--palette-body-header-image);
        background-repeat: var(--palette-body-header-repeat);
        background-position: var(--palette-body-header-position);
        background-size: var(--palette-body-header-size);
        background-attachment: var(--palette-body-header-attachment);
    }
    """;

    private static string BuildAutoBodyPaletteVariablesBlocks(
        PaletteDefinition definition,
        PaletteOptions options)
    {
        string autoDarkBlock = BuildBodyPaletteVariablesBlock(
            GetAutoBodyDarkSelectors(options),
            definition,
            PaletteMode.Dark,
            options
        );

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            new[]
            {
                BuildBodyPaletteVariablesBlock(
                    GetBodySelector(options),
                    definition,
                    PaletteMode.Light,
                    options
                ),

                $$"""
                @media (prefers-color-scheme: dark) {
                {{Indent(autoDarkBlock, 4)}}
                }
                """,

                BuildBodyPaletteVariablesBlock(
                    GetExplicitBodyDarkSelectors(options),
                    definition,
                    PaletteMode.Dark,
                    options
                ),

                BuildBodyPaletteVariablesBlock(
                    GetExplicitBodyLightSelectors(options),
                    definition,
                    PaletteMode.Light,
                    options
                )
            }.Where(section => !string.IsNullOrWhiteSpace(section))
        );
    }

    private static string BuildBodyPaletteVariablesBlock(
        string selector,
        PaletteDefinition definition,
        PaletteMode mode,
        PaletteOptions options) => $$"""
    {{selector}} {
    {{Indent(GetCssVariables(definition, mode, options), 4)}}
    }
    """;

    private static string GetBodyCssVariables(
        PaletteDefinition definition,
        PaletteMode mode,
        PaletteOptions options)
    {
        bool isDark = mode == PaletteMode.Dark;

        string pageBackground = isDark
            ? options.DarkPageBackground
            : options.LightPageBackground;

        string headerImageUrl = BuildHeaderImageUrl(definition, mode, options);

        string headerImageCss = options.IncludeHeaderImages
            ? CssUrl(headerImageUrl)
            : "none";

        return $$"""
        --palette-body-bg: {{pageBackground}};
        --palette-body-header-image: {{headerImageCss}};
        --palette-body-header-repeat: no-repeat;
        --palette-body-header-position: {{options.HeaderBackgroundPosition}};
        --palette-body-header-size: {{options.HeaderBackgroundSize}};
        --palette-body-header-attachment: {{options.HeaderBackgroundAttachment}};
        """;
    }

    private static string GetCssVariables(
        PaletteDefinition definition,
        PaletteMode mode,
        PaletteOptions options)
    {
        bool isDark = mode == PaletteMode.Dark;

        PaletteTokens tokens = isDark ? definition.Dark : definition.Light;

        string colorScheme = isDark ? "dark" : "light";
        string text = isDark ? tokens.TextLight : tokens.TextDark;
        string textInverse = isDark ? tokens.TextDark : tokens.TextLight;
        string isDarkValue = isDark ? "1" : "0";
        string pageBackground = isDark ? options.DarkPageBackground : options.LightPageBackground;
        string headerImageUrl = BuildHeaderImageUrl(definition, mode, options);
        string headerImageCss = options.IncludeHeaderImages
            ? CssUrl(headerImageUrl)
            : "none";

        return $$"""
        color-scheme: {{colorScheme}};

        --palette-mode-dark: {{isDarkValue}};

        --palette-page-bg: {{pageBackground}};
        --palette-body-bg: {{pageBackground}};

        --palette-body-header-image: {{headerImageCss}};
        --palette-body-header-repeat: no-repeat;
        --palette-body-header-position: {{options.HeaderBackgroundPosition}};
        --palette-body-header-size: {{options.HeaderBackgroundSize}};
        --palette-body-header-attachment: {{options.HeaderBackgroundAttachment}};

        --palette-surface-bg: {{tokens.SurfaceBg}};
        --palette-surface-start: {{tokens.SurfaceStart}};
        --palette-surface-end: {{tokens.SurfaceEnd}};
        --palette-surface-glow: {{tokens.SurfaceGlow}};

        --palette-tone-100: {{tokens.Tone100}};
        --palette-tone-300: {{tokens.Tone300}};
        --palette-tone-700: {{tokens.Tone700}};
        --palette-tone-800: {{tokens.Tone800}};
        --palette-tone-900: {{tokens.Tone900}};
        --palette-tone-950: {{tokens.Tone950}};

        --palette-text: {{text}};
        --palette-text-inverse: {{textInverse}};
        --palette-text-light: {{tokens.TextLight}};
        --palette-text-dark: {{tokens.TextDark}};
        """;
    }

    private static string BuildHeaderImageUrl(
        PaletteDefinition definition,
        PaletteMode mode,
        PaletteOptions options)
    {
        string modeName = mode == PaletteMode.Dark ? "dark" : "light";
        string extension = options.HeaderImageExtension.TrimStart('.');

        string fileName = options.HeaderImageFilePattern
            .Replace("{palette}", definition.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{mode}", modeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{extension}", extension, StringComparison.OrdinalIgnoreCase)
            .Replace("{css-class}", definition.CssClass, StringComparison.OrdinalIgnoreCase);

        string basePath = options.HeaderImageBasePath.TrimEnd('/');
        fileName = fileName.TrimStart('/');

        return string.IsNullOrWhiteSpace(basePath)
            ? fileName
            : $"{basePath}/{fileName}";
    }

    private static string CssUrl(string value) =>
        $"url(\"{EscapeCssString(value)}\")";

    private static string EscapeCssString(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string GetBodySelector(PaletteOptions options) =>
        options.BodySelector;

    private static string GetAutoBodyDarkSelectors(PaletteOptions options)
    {
        string bodySelector = GetBodySelector(options);

        return $$"""
        :root:not(.{{PaletteLightClass}}):not(.light):not([data-theme="light"]) {{bodySelector}}:not(.{{PaletteLightClass}}):not(.light):not([data-theme="light"]),
        .{{PaletteAutoClass}} {{bodySelector}}:not(.{{PaletteLightClass}}):not(.light):not([data-theme="light"]),
        {{bodySelector}}.{{PaletteAutoClass}}
        """;
    }

    private static string GetExplicitBodyDarkSelectors(PaletteOptions options)
    {
        string bodySelector = GetBodySelector(options);

        return $$"""
        .{{PaletteDarkClass}} {{bodySelector}},
        [data-theme="dark"] {{bodySelector}},
        .dark {{bodySelector}},
        {{bodySelector}}.{{PaletteDarkClass}},
        {{bodySelector}}[data-theme="dark"],
        {{bodySelector}}.dark
        """;
    }

    private static string GetExplicitBodyLightSelectors(PaletteOptions options)
    {
        string bodySelector = GetBodySelector(options);

        return $$"""
        .{{PaletteLightClass}} {{bodySelector}},
        [data-theme="light"] {{bodySelector}},
        .light {{bodySelector}},
        {{bodySelector}}.{{PaletteLightClass}},
        {{bodySelector}}[data-theme="light"],
        {{bodySelector}}.light
        """;
    }

    private static string GetBaseLightSelectors(PaletteDefinition definition) => definition.IsDefault
        ? $$"""
        :root,
        .{{definition.CssClass}},
        [data-palette="{{definition.Name}}"]
        """
        : $$"""
        .{{definition.CssClass}},
        [data-palette="{{definition.Name}}"]
        """;

    private static string GetAutoDarkSelectors(PaletteDefinition definition) => definition.IsDefault
        ? $$"""
        :root:not(.{{PaletteLightClass}}):not(.light):not([data-theme="light"]),
        .{{PaletteAutoClass}}.{{definition.CssClass}},
        .{{definition.CssClass}}.{{PaletteAutoClass}},
        .{{definition.CssClass}}:not(.{{PaletteLightClass}}),
        [data-palette="{{definition.Name}}"]:not(.{{PaletteLightClass}})
        """
        : $$"""
        .{{PaletteAutoClass}}.{{definition.CssClass}},
        .{{definition.CssClass}}.{{PaletteAutoClass}},
        .{{definition.CssClass}}:not(.{{PaletteLightClass}}),
        [data-palette="{{definition.Name}}"]:not(.{{PaletteLightClass}})
        """;

    private static string GetExplicitDarkSelectors(PaletteDefinition definition) => definition.IsDefault
        ? $$"""
        .{{PaletteDarkClass}},
        [data-theme="dark"],
        .dark,
        .{{PaletteDarkClass}}.{{definition.CssClass}},
        .{{PaletteDarkClass}} .{{definition.CssClass}},
        [data-theme="dark"].{{definition.CssClass}},
        [data-theme="dark"] .{{definition.CssClass}},
        .dark.{{definition.CssClass}},
        .dark .{{definition.CssClass}},
        .{{PaletteDarkClass}}[data-palette="{{definition.Name}}"],
        .{{PaletteDarkClass}} [data-palette="{{definition.Name}}"],
        [data-theme="dark"][data-palette="{{definition.Name}}"],
        [data-theme="dark"] [data-palette="{{definition.Name}}"],
        .dark[data-palette="{{definition.Name}}"],
        .dark [data-palette="{{definition.Name}}"]
        """
        : $$"""
        .{{PaletteDarkClass}}.{{definition.CssClass}},
        .{{PaletteDarkClass}} .{{definition.CssClass}},
        [data-theme="dark"].{{definition.CssClass}},
        [data-theme="dark"] .{{definition.CssClass}},
        .dark.{{definition.CssClass}},
        .dark .{{definition.CssClass}},
        .{{PaletteDarkClass}}[data-palette="{{definition.Name}}"],
        .{{PaletteDarkClass}} [data-palette="{{definition.Name}}"],
        [data-theme="dark"][data-palette="{{definition.Name}}"],
        [data-theme="dark"] [data-palette="{{definition.Name}}"],
        .dark[data-palette="{{definition.Name}}"],
        .dark [data-palette="{{definition.Name}}"]
        """;

    private static string GetExplicitLightSelectors(PaletteDefinition definition) => definition.IsDefault
        ? $$"""
        .{{PaletteLightClass}},
        [data-theme="light"],
        .light,
        .{{PaletteLightClass}}.{{definition.CssClass}},
        .{{PaletteLightClass}} .{{definition.CssClass}},
        [data-theme="light"].{{definition.CssClass}},
        [data-theme="light"] .{{definition.CssClass}},
        .light.{{definition.CssClass}},
        .light .{{definition.CssClass}},
        .{{PaletteLightClass}}[data-palette="{{definition.Name}}"],
        .{{PaletteLightClass}} [data-palette="{{definition.Name}}"],
        [data-theme="light"][data-palette="{{definition.Name}}"],
        [data-theme="light"] [data-palette="{{definition.Name}}"],
        .light[data-palette="{{definition.Name}}"],
        .light [data-palette="{{definition.Name}}"]
        """
        : $$"""
        .{{PaletteLightClass}}.{{definition.CssClass}},
        .{{PaletteLightClass}} .{{definition.CssClass}},
        [data-theme="light"].{{definition.CssClass}},
        [data-theme="light"] .{{definition.CssClass}},
        .light.{{definition.CssClass}},
        .light .{{definition.CssClass}},
        .{{PaletteLightClass}}[data-palette="{{definition.Name}}"],
        .{{PaletteLightClass}} [data-palette="{{definition.Name}}"],
        [data-theme="light"][data-palette="{{definition.Name}}"],
        [data-theme="light"] [data-palette="{{definition.Name}}"],
        .light[data-palette="{{definition.Name}}"],
        .light [data-palette="{{definition.Name}}"]
        """;

    private static string Indent(string value, int spaces)
    {
        string prefix = new(' ', spaces);
        string[] lines = value.Replace("\r\n", "\n").Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > 0)
                lines[i] = prefix + lines[i];
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static PaletteDefinition GetDefinition(Palettes palette) => palette switch
    {
        Palettes.Pink => Pink,
        Palettes.Yellow => Yellow,
        Palettes.Blue => Blue,
        Palettes.Purple => Purple,
        Palettes.Lagoon => Lagoon,
        Palettes.Sunset => Sunset,
        _ => Green
    };

    public enum PaletteMode
    {
        Auto,
        Light,
        Dark
    }

    public sealed record PaletteOptions
    {
        public static PaletteOptions Default { get; } = new();

        public string HeaderImageBasePath { get; init; } = "/bg/headers";

        public string HeaderImageFilePattern { get; init; } = "{palette}-{mode}-wide.{extension}";

        public string HeaderImageExtension { get; init; } = "webp";

        public bool IncludeHeaderImages { get; init; } = true;

        public bool IncludeBodyBackgroundCss { get; init; } = true;

        public string BodySelector { get; init; } = "body";

        public Palettes? BodyPalette { get; init; }

        public PaletteMode BodyPaletteMode { get; init; } = PaletteMode.Auto;

        public string LightPageBackground { get; init; } = "#ffffff";

        public string DarkPageBackground { get; init; } = "#2B2B2B";

        public string HeaderBackgroundPosition { get; init; } = "top center";

        public string HeaderBackgroundSize { get; init; } = "100% auto";

        public string HeaderBackgroundAttachment { get; init; } = "scroll";
    }

    private static readonly PaletteTokens GreenLightTokens = new(
        SurfaceBg: "#055b33",
        SurfaceStart: "#9dcd2e",
        SurfaceEnd: "#032d20",
        SurfaceGlow: "rgba(157, 205, 46, 0.30)",
        Tone100: "#ddff8d",
        Tone300: "#9dcd2e",
        Tone700: "#66ab2a",
        Tone800: "#298c33",
        Tone900: "#055b33",
        Tone950: "#032d20",
        TextLight: "#efffd2",
        TextDark: "#032d20"
    );

    private static readonly PaletteTokens GreenDarkTokens = new(
        SurfaceBg: "#063d2a",
        SurfaceStart: "#20872e",
        SurfaceEnd: "#001b16",
        SurfaceGlow: "rgba(157, 205, 46, 0.20)",
        Tone100: "#ddff8d",
        Tone300: "#96cb21",
        Tone700: "#20872e",
        Tone800: "#0d7237",
        Tone900: "#085b31",
        Tone950: "#001b16",
        TextLight: "#efffd2",
        TextDark: "#001b16"
    );

    private static readonly PaletteTokens PinkLightTokens = new(
SurfaceBg: "#979cd0", SurfaceStart: "#75c6ee", SurfaceEnd: "#dcaad3", SurfaceGlow: "rgba(151, 156, 208, 0.28)", Tone100: "#f8e1f5", Tone300: "#e4b9dd", Tone700: "#979cd0", Tone800: "#489cdd", Tone900: "#4a5f92", Tone950: "#28345d", TextLight: "#fbf6ff", TextDark: "#28345d"
    );

    private static readonly PaletteTokens PinkDarkTokens = new(
SurfaceBg: "#28345d", SurfaceStart: "#438dce", SurfaceEnd: "#171b34", SurfaceGlow: "rgba(151, 156, 208, 0.20)", Tone100: "#f8e1f5", Tone300: "#dfadd8", Tone700: "#8d94c9", Tone800: "#438dce", Tone900: "#475d7e", Tone950: "#171b34", TextLight: "#fbf6ff", TextDark: "#171b34"
    );

    private static readonly PaletteTokens YellowLightTokens = new(
        SurfaceBg: "#d77507",
        SurfaceStart: "#f2ba40",
        SurfaceEnd: "#863c0b",
        SurfaceGlow: "rgba(242, 186, 64, 0.30)",
        Tone100: "#fff2b3",
        Tone300: "#f2ba40",
        Tone700: "#e0950b",
        Tone800: "#ca5b0a",
        Tone900: "#863c0b",
        Tone950: "#3e1900",
        TextLight: "#fff8dc",
        TextDark: "#3e1900"
    );

    private static readonly PaletteTokens YellowDarkTokens = new(
        SurfaceBg: "#5d290c",
        SurfaceStart: "#dd8e05",
        SurfaceEnd: "#1f1000",
        SurfaceGlow: "rgba(242, 186, 64, 0.22)",
        Tone100: "#fff2b3",
        Tone300: "#eead2a",
        Tone700: "#d16f05",
        Tone800: "#c05108",
        Tone900: "#5d290c",
        Tone950: "#1f1000",
        TextLight: "#fff8dc",
        TextDark: "#1f1000"
    );

    private static readonly PaletteTokens BlueLightTokens = new(
        SurfaceBg: "#0c61ad",
        SurfaceStart: "#37c0f7",
        SurfaceEnd: "#041447",
        SurfaceGlow: "rgba(55, 192, 247, 0.28)",
        Tone100: "#cfffff",
        Tone300: "#37c0f7",
        Tone700: "#1d97da",
        Tone800: "#0c61ad",
        Tone900: "#153887",
        Tone950: "#041447",
        TextLight: "#eaffff",
        TextDark: "#041447"
    );

    private static readonly PaletteTokens BlueDarkTokens = new(
        SurfaceBg: "#091e50",
        SurfaceStart: "#1493dc",
        SurfaceEnd: "#000629",
        SurfaceGlow: "rgba(55, 192, 247, 0.22)",
        Tone100: "#cfffff",
        Tone300: "#2dbef9",
        Tone700: "#0a72bf",
        Tone800: "#0862b2",
        Tone900: "#0d317e",
        Tone950: "#000629",
        TextLight: "#eaffff",
        TextDark: "#000629"
    );

        private static readonly PaletteTokens PurpleLightTokens = new(
        SurfaceBg: "#b22b75",
        SurfaceStart: "#ed4d89",
        SurfaceEnd: "#2d063c",
        SurfaceGlow: "rgba(237, 77, 137, 0.30)",
        Tone100: "#fde6f1",
        Tone300: "#f58abb",
        Tone700: "#b22b75",
        Tone800: "#871a62",
        Tone900: "#590d51",
        Tone950: "#23032d",
        TextLight: "#fff7fb",
        TextDark: "#23032d"
    );

        private static readonly PaletteTokens PurpleDarkTokens = new(
            SurfaceBg: "#59054e",
            SurfaceStart: "#e43786",
            SurfaceEnd: "#270235",
            SurfaceGlow: "rgba(228, 55, 134, 0.24)",
            Tone100: "#fde6f1",
            Tone300: "#f06aa4",
            Tone700: "#b91c79",
            Tone800: "#840d64",
            Tone900: "#59054e",
            Tone950: "#270235",
            TextLight: "#fff7fb",
            TextDark: "#270235"
        );

    private static readonly PaletteTokens LagoonLightTokens = new(
        SurfaceBg: "#93beba",
        SurfaceStart: "#bde1df",
        SurfaceEnd: "#d2bc9f",
        SurfaceGlow: "rgba(189, 225, 223, 0.28)",
        Tone100: "#f4ead6",
        Tone300: "#bde1df",
        Tone700: "#93beba",
        Tone800: "#588485",
        Tone900: "#415f61",
        Tone950: "#253b3d",
        TextLight: "#f7fffd",
        TextDark: "#253b3d"
    );

    private static readonly PaletteTokens LagoonDarkTokens = new(
        SurfaceBg: "#415f61",
        SurfaceStart: "#a7d5d1",
        SurfaceEnd: "#5e675d",
        SurfaceGlow: "rgba(167, 213, 209, 0.20)",
        Tone100: "#f4ead6",
        Tone300: "#a7d5d1",
        Tone700: "#93c9c5",
        Tone800: "#6d9694",
        Tone900: "#5e675d",
        Tone950: "#253b3d",
        TextLight: "#f7fffd",
        TextDark: "#253b3d"
    );

    private static readonly PaletteTokens SunsetLightTokens = new(
        SurfaceBg: "#b2077f",
        SurfaceStart: "#f7ba28",
        SurfaceEnd: "#790368",
        SurfaceGlow: "rgba(247, 186, 40, 0.30)",
        Tone100: "#ffe08a",
        Tone300: "#f7ba28",
        Tone700: "#f05351",
        Tone800: "#d31176",
        Tone900: "#960479",
        Tone950: "#300026",
        TextLight: "#fff6e2",
        TextDark: "#300026"
    );

    private static readonly PaletteTokens SunsetDarkTokens = new(
        SurfaceBg: "#490f45",
        SurfaceStart: "#f59a2c",
        SurfaceEnd: "#2B2B2B",
        SurfaceGlow: "rgba(245, 154, 44, 0.22)",
        Tone100: "#ffe08a",
        Tone300: "#f59a2c",
        Tone700: "#e42b4f",
        Tone800: "#d41371",
        Tone900: "#8c057b",
        Tone950: "#2B2B2B",
        TextLight: "#fff6e2",
        TextDark: "#2B2B2B"
    );

    private static readonly PaletteTone[] ToneOrder =
    [
        PaletteTone.Tone100,
        PaletteTone.Tone300,
        PaletteTone.Tone700,
        PaletteTone.Tone800,
        PaletteTone.Tone900,
        PaletteTone.Tone950
    ];

    private static readonly PaletteDefinition Green = new(
        Palette: default,
        Name: "green",
        CssClass: "palette-green",
        IsDefault: true,
        Light: GreenLightTokens,
        Dark: GreenDarkTokens
    );

    private static readonly PaletteDefinition Pink = new(
        Palette: Palettes.Pink,
        Name: "pink",
        CssClass: "palette-pink",
        IsDefault: false,
        Light: PinkLightTokens,
        Dark: PinkDarkTokens
    );

    private static readonly PaletteDefinition Yellow = new(
        Palette: Palettes.Yellow,
        Name: "yellow",
        CssClass: "palette-yellow",
        IsDefault: false,
        Light: YellowLightTokens,
        Dark: YellowDarkTokens
    );

    private static readonly PaletteDefinition Blue = new(
        Palette: Palettes.Blue,
        Name: "blue",
        CssClass: "palette-blue",
        IsDefault: false,
        Light: BlueLightTokens,
        Dark: BlueDarkTokens
    );

    private static readonly PaletteDefinition Purple = new(
        Palette: Palettes.Purple,
        Name: "purple",
        CssClass: "palette-purple",
        IsDefault: false,
        Light: PurpleLightTokens,
        Dark: PurpleDarkTokens
    );

    private static readonly PaletteDefinition Lagoon = new(
        Palette: Palettes.Lagoon,
        Name: "lagoon",
        CssClass: "palette-lagoon",
        IsDefault: false,
        Light: LagoonLightTokens,
        Dark: LagoonDarkTokens
    );

    private static readonly PaletteDefinition Sunset = new(
        Palette: Palettes.Sunset,
        Name: "sunset",
        CssClass: "palette-sunset",
        IsDefault: false,
        Light: SunsetLightTokens,
        Dark: SunsetDarkTokens
    );

    private static readonly PaletteDefinition[] Definitions =
    [
        Green,
        Pink,
        Yellow,
        Blue,
        Purple,
        Lagoon,
        Sunset
    ];

    private sealed record PaletteDefinition(
        Palettes Palette,
        string Name,
        string CssClass,
        bool IsDefault,
        PaletteTokens Light,
        PaletteTokens Dark
    );

    private sealed record PaletteTokens(
        string SurfaceBg,
        string SurfaceStart,
        string SurfaceEnd,
        string SurfaceGlow,
        string Tone100,
        string Tone300,
        string Tone700,
        string Tone800,
        string Tone900,
        string Tone950,
        string TextLight,
        string TextDark
    );
}
