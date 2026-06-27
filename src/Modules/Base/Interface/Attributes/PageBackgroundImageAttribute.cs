using Vitarish.Modules.Base.Interface.Extensions;

namespace Vitarish.Modules.Base.Interface.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PageBackgroundImageAttribute : Attribute
{
    public PageBackgroundImageAttribute(string fileNamePrefix)
    {
        if (string.IsNullOrWhiteSpace(fileNamePrefix))
        {
            throw new ArgumentException("Background image file name prefix cannot be empty.", nameof(fileNamePrefix));
        }

        FileNamePrefix = fileNamePrefix;
    }

    /// <summary>
    /// File name prefix without the -light/-dark suffix.
    /// Example: /bg/headers/intuition produces:
    /// /bg/headers/intuition-light.webp and /bg/headers/intuition-dark.webp.
    /// </summary>
    public string FileNamePrefix { get; }

    public string Extension { get; set; } = PalettePageBackgroundImages.DefaultExtension;

    public bool IncludeHeaderImages { get; set; } = true;

    public string? Position { get; set; }

    public string? Size { get; set; }

    public string? Attachment { get; set; }
}
