using System.Globalization;
using System.Reflection;
using Vitarish.Modules.Base.Interface.Attributes;

namespace Vitarish.Modules.Base.Interface.Extensions;

public static class PalettePageBackgroundImages
{
    public const string DefaultExtension = "webp";

    public const string Intuition = "/bg/headers/intuition";
    public const string Knowledge = "/bg/headers/knowledge";
    public const string Medical = "/bg/headers/medical";
    public const string Fitness = "/bg/headers/fitness";

    public static PaletteCss.PaletteOptions ApplyPageBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        Type? pageType)
    {
        PageBackgroundImageAttribute? attribute = pageType?
            .GetCustomAttribute<PageBackgroundImageAttribute>(inherit: true);

        return options.ApplyPageBackgroundImage(attribute);
    }

    public static PaletteCss.PaletteOptions ApplyPageBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        PageBackgroundImageAttribute? attribute)
    {
        PaletteCss.PaletteOptions resolvedOptions = options ?? PaletteCss.PaletteOptions.Default;

        if (attribute is null)
        {
            return resolvedOptions;
        }

        resolvedOptions = resolvedOptions with
        {
            IncludeHeaderImages = attribute.IncludeHeaderImages
        };

        if (!string.IsNullOrWhiteSpace(attribute.Position))
        {
            resolvedOptions = resolvedOptions with
            {
                HeaderBackgroundPosition = attribute.Position.Trim()
            };
        }

        if (!string.IsNullOrWhiteSpace(attribute.Size))
        {
            resolvedOptions = resolvedOptions with
            {
                HeaderBackgroundSize = attribute.Size.Trim()
            };
        }

        if (!string.IsNullOrWhiteSpace(attribute.Attachment))
        {
            resolvedOptions = resolvedOptions with
            {
                HeaderBackgroundAttachment = attribute.Attachment.Trim()
            };
        }

        if (!attribute.IncludeHeaderImages)
        {
            return resolvedOptions;
        }

        return resolvedOptions.WithBackgroundImagePrefix(
            attribute.FileNamePrefix,
            attribute.Extension);
    }

    public static PaletteCss.PaletteOptions WithIntuitionBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        string extension = DefaultExtension) =>
        options.WithBackgroundImagePrefix(Intuition, extension);

    public static PaletteCss.PaletteOptions WithKnowledgeBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        string extension = DefaultExtension) =>
        options.WithBackgroundImagePrefix(Knowledge, extension);

    public static PaletteCss.PaletteOptions WithMedicalBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        string extension = DefaultExtension) =>
        options.WithBackgroundImagePrefix(Medical, extension);

    public static PaletteCss.PaletteOptions WithFitnessBackgroundImage(
        this PaletteCss.PaletteOptions? options,
        string extension = DefaultExtension) =>
        options.WithBackgroundImagePrefix(Fitness, extension);


    public static PaletteCss.PaletteOptions WithBackgroundImagePrefix(
        this PaletteCss.PaletteOptions? options,
        string fileNamePrefix,
        string extension = DefaultExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNamePrefix))
        {
            throw new ArgumentException("Background image file name prefix cannot be empty.", nameof(fileNamePrefix));
        }

        string normalizedPrefix = NormalizePrefix(fileNamePrefix);
        string normalizedExtension = NormalizeExtension(extension);

        return (options ?? PaletteCss.PaletteOptions.Default).WithHeaderImageUrlResolver(context =>
            BuildBackgroundImageUrl(normalizedPrefix, context.Mode, normalizedExtension));
    }

    public static string GetBackgroundImageUrl(
        string fileNamePrefix,
        PaletteCss.PaletteMode mode,
        string extension = DefaultExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNamePrefix))
        {
            throw new ArgumentException("Background image file name prefix cannot be empty.", nameof(fileNamePrefix));
        }

        return BuildBackgroundImageUrl(
            NormalizePrefix(fileNamePrefix),
            mode,
            NormalizeExtension(extension));
    }

    private static string BuildBackgroundImageUrl(
        string normalizedPrefix,
        PaletteCss.PaletteMode mode,
        string normalizedExtension)
    {
        string modeName = mode == PaletteCss.PaletteMode.Dark ? "dark" : "light";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{normalizedPrefix}-{modeName}.{normalizedExtension}");
    }

    private static string NormalizePrefix(string fileNamePrefix) =>
        fileNamePrefix.Trim().TrimEnd('/');

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Background image extension cannot be empty.", nameof(extension));
        }

        return extension.Trim().TrimStart('.');
    }
}
