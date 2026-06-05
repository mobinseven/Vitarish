using Microsoft.EntityFrameworkCore;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Infrastructure.Data;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class PlatformThemeStore(PlatformDbContext dbContext, TimeProvider timeProvider) : IPlatformThemeQuery, IPlatformThemeCommand
{
    private const int ThemeSelectionId = 1;
    private const string NameRequired = "Theme name is required.";
    private const string ColorRequired = "All palette colors are required.";
    private const string ColorInvalid = "Palette colors must be valid hex values.";
    private const string ThemeNotFound = "Theme was not found.";
    private const string ThemeNameExists = "Theme name already exists.";
    public const string ActiveThemeDeleteError = "Active theme cannot be deleted.";

    public async Task<IReadOnlyList<ThemeListItem>> ListThemesAsync(CancellationToken cancellationToken = default)
    {
        ThemeSelection? selection = await dbContext.ThemeSelections.AsNoTracking().SingleOrDefaultAsync(e => e.Id == ThemeSelectionId, cancellationToken);
        return await dbContext.Themes
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => new ThemeListItem(e.Id, e.Name, ToDto(e.LightPalette), ToDto(e.DarkPalette), selection != null && e.Id == selection.ActiveThemeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ThemeEditorModel?> GetThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        Theme? theme = await dbContext.Themes.AsNoTracking().SingleOrDefaultAsync(e => e.Id == themeId, cancellationToken);
        return theme is null ? null : ToEditorModel(theme);
    }

    public async Task<ThemeEditorModel?> GetActiveThemeAsync(CancellationToken cancellationToken = default)
    {
        ThemeSelection? selection = await dbContext.ThemeSelections.AsNoTracking().SingleOrDefaultAsync(e => e.Id == ThemeSelectionId, cancellationToken);
        if (selection is null)
        {
            return null;
        }

        Theme? theme = await dbContext.Themes.AsNoTracking().SingleOrDefaultAsync(e => e.Id == selection.ActiveThemeId, cancellationToken);
        return theme is null ? null : ToEditorModel(theme);
    }

    public async Task<CreateThemeResult> CreateThemeAsync(ThemeWriteModel model, CancellationToken cancellationToken = default)
    {
        string? validationError = Validate(model);
        if (validationError is not null)
        {
            return CreateThemeResult.Failure(validationError);
        }

        string name = model.Name.Trim();
        bool exists = await dbContext.Themes.AnyAsync(e => e.Name == name, cancellationToken);
        if (exists)
        {
            return CreateThemeResult.Failure(ThemeNameExists);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        Theme theme = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            LightPalette = ToEntity(model.LightPalette),
            DarkPalette = ToEntity(model.DarkPalette),
            CreatedAt = now,
            UpdatedAt = now
        };

        _ = dbContext.Themes.Add(theme);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CreateThemeResult.Success(theme.Id);
    }

    public async Task<PlatformCommandResult> UpdateThemeAsync(Guid themeId, ThemeWriteModel model, CancellationToken cancellationToken = default)
    {
        string? validationError = Validate(model);
        if (validationError is not null)
        {
            return PlatformCommandResult.Failure(validationError);
        }

        Theme? theme = await dbContext.Themes.SingleOrDefaultAsync(e => e.Id == themeId, cancellationToken);
        if (theme is null)
        {
            return PlatformCommandResult.Failure(ThemeNotFound);
        }

        string name = model.Name.Trim();
        bool exists = await dbContext.Themes.AnyAsync(e => e.Id != themeId && e.Name == name, cancellationToken);
        if (exists)
        {
            return PlatformCommandResult.Failure(ThemeNameExists);
        }

        theme.Name = name;
        theme.LightPalette = ToEntity(model.LightPalette);
        theme.DarkPalette = ToEntity(model.DarkPalette);
        theme.UpdatedAt = timeProvider.GetUtcNow();
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PlatformCommandResult.Success();
    }

    public async Task<PlatformCommandResult> SetActiveThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        bool themeExists = await dbContext.Themes.AnyAsync(e => e.Id == themeId, cancellationToken);
        if (!themeExists)
        {
            return PlatformCommandResult.Failure(ThemeNotFound);
        }

        ThemeSelection? selection = await dbContext.ThemeSelections.SingleOrDefaultAsync(e => e.Id == ThemeSelectionId, cancellationToken);
        if (selection is null)
        {
            selection = new ThemeSelection { Id = ThemeSelectionId };
            _ = dbContext.ThemeSelections.Add(selection);
        }

        selection.ActiveThemeId = themeId;
        selection.UpdatedAt = timeProvider.GetUtcNow();
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PlatformCommandResult.Success();
    }

    public async Task<PlatformCommandResult> DeleteInactiveThemeAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        PlatformCommandResult deletionCheck = await CheckDeleteAllowedAsync(themeId, cancellationToken);
        if (!deletionCheck.Succeeded)
        {
            return deletionCheck;
        }

        Theme? theme = await dbContext.Themes.SingleOrDefaultAsync(e => e.Id == themeId, cancellationToken);
        if (theme is null)
        {
            return PlatformCommandResult.Failure(ThemeNotFound);
        }

        _ = dbContext.Themes.Remove(theme);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PlatformCommandResult.Success();
    }

    private async Task<PlatformCommandResult> CheckDeleteAllowedAsync(Guid themeId, CancellationToken cancellationToken)
    {
        ThemeSelection? selection = await dbContext.ThemeSelections.AsNoTracking().SingleOrDefaultAsync(e => e.Id == ThemeSelectionId, cancellationToken);
        if (selection is not null && selection.ActiveThemeId == themeId)
        {
            return PlatformCommandResult.Failure(ActiveThemeDeleteError);
        }

        return PlatformCommandResult.Success();
    }

    private static string? Validate(ThemeWriteModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return NameRequired;
        }

        if (PaletteValues(model.LightPalette).Concat(PaletteValues(model.DarkPalette)).Any(string.IsNullOrWhiteSpace))
        {
            return ColorRequired;
        }

        if (PaletteValues(model.LightPalette).Concat(PaletteValues(model.DarkPalette)).Any(v => !IsHexColor(v)))
        {
            return ColorInvalid;
        }

        return null;
    }

    private static bool IsHexColor(string value)
        => value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

    private static IEnumerable<string> PaletteValues(ThemePaletteDto palette)
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

    private static ThemeEditorModel ToEditorModel(Theme theme)
        => new(theme.Id, theme.Name, ToDto(theme.LightPalette), ToDto(theme.DarkPalette));

    private static ThemePaletteDto ToDto(ThemePalette palette)
        => new(palette.Primary, palette.PrimaryContrastText, palette.Secondary, palette.SecondaryContrastText, palette.Background, palette.Surface, palette.AppbarBackground, palette.AppbarText, palette.TextPrimary, palette.TextSecondary, palette.Divider);

    private static ThemePalette ToEntity(ThemePaletteDto palette)
        => new()
        {
            Primary = palette.Primary.Trim().ToUpperInvariant(),
            PrimaryContrastText = palette.PrimaryContrastText.Trim().ToUpperInvariant(),
            Secondary = palette.Secondary.Trim().ToUpperInvariant(),
            SecondaryContrastText = palette.SecondaryContrastText.Trim().ToUpperInvariant(),
            Background = palette.Background.Trim().ToUpperInvariant(),
            Surface = palette.Surface.Trim().ToUpperInvariant(),
            AppbarBackground = palette.AppbarBackground.Trim().ToUpperInvariant(),
            AppbarText = palette.AppbarText.Trim().ToUpperInvariant(),
            TextPrimary = palette.TextPrimary.Trim().ToUpperInvariant(),
            TextSecondary = palette.TextSecondary.Trim().ToUpperInvariant(),
            Divider = palette.Divider.Trim().ToUpperInvariant()
        };
}
