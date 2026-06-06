using Microsoft.EntityFrameworkCore;
using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Application.Rules;
using Vitarish.Platform.Application.Validation;
using Vitarish.Platform.Domain.Entities;
using Vitarish.Platform.Domain.Models;
using Vitarish.Platform.Infrastructure.Data;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class PlatformThemeStore(PlatformDbContext dbContext, TimeProvider timeProvider) : IPlatformThemeQuery, IPlatformThemeCommand
{
    private const int ThemeSelectionId = 1;
    private const string ThemeNotFound = "Theme was not found.";
    private const string ThemeNameExists = "Theme name already exists.";

    public async Task<IReadOnlyList<ThemeListItem>> ListThemesAsync(CancellationToken cancellationToken = default)
    {
        ThemeSelection? selection = await dbContext.ThemeSelections.AsNoTracking().SingleOrDefaultAsync(e => e.Id == ThemeSelectionId, cancellationToken);
        List<Theme> themes = await dbContext.Themes
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);

        return themes
            .Select(e => new ThemeListItem(
                e.Id,
                e.Name,
                CopyPalette(e.LightPalette, new ThemePalette()),
                CopyPalette(e.DarkPalette, DefaultPlatformTheme.CreateDarkPalette()),
                selection != null && e.Id == selection.ActiveThemeId))
            .ToList();
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
        string? validationError = ThemeWriteValidator.Validate(model);
        if (validationError is not null)
        {
            return CreateThemeResult.Failure(validationError);
        }

        string name = model.Name.Trim();
        string normalizedName = name.ToLowerInvariant();
        bool exists = await dbContext.Themes.AnyAsync(e => e.Name.ToLower() == normalizedName, cancellationToken);
        if (exists)
        {
            return CreateThemeResult.Failure(ThemeNameExists);
        }

        Theme theme = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            LightPalette = NormalizePalette(model.LightPalette),
            DarkPalette = NormalizePalette(model.DarkPalette)
        };

        _ = dbContext.Themes.Add(theme);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CreateThemeResult.Success(theme.Id);
    }

    public async Task<PlatformCommandResult> UpdateThemeAsync(Guid themeId, ThemeWriteModel model, CancellationToken cancellationToken = default)
    {
        string? validationError = ThemeWriteValidator.Validate(model);
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
        string normalizedName = name.ToLowerInvariant();
        bool exists = await dbContext.Themes.AnyAsync(e => e.Id != themeId && e.Name.ToLower() == normalizedName, cancellationToken);
        if (exists)
        {
            return PlatformCommandResult.Failure(ThemeNameExists);
        }

        theme.Name = name;
        theme.LightPalette = NormalizePalette(model.LightPalette);
        theme.DarkPalette = NormalizePalette(model.DarkPalette);
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
        string? deleteError = ThemeDeletionPolicy.ValidateInactiveDeletion(themeId, selection?.ActiveThemeId);
        return deleteError is null
            ? PlatformCommandResult.Success()
            : PlatformCommandResult.Failure(deleteError);
    }

    private static ThemeEditorModel ToEditorModel(Theme theme)
        => new(
            theme.Id,
            theme.Name,
            CopyPalette(theme.LightPalette, new ThemePalette()),
            CopyPalette(theme.DarkPalette, DefaultPlatformTheme.CreateDarkPalette()));

    private static ThemePalette CopyPalette(ThemePalette? palette, ThemePalette fallback)
    {
        ThemePalette source = palette ?? fallback;

        return new()
        {
            Primary = ColorOrFallback(source.Primary, fallback.Primary),
            PrimaryContrastText = ColorOrFallback(source.PrimaryContrastText, fallback.PrimaryContrastText),
            Secondary = ColorOrFallback(source.Secondary, fallback.Secondary),
            SecondaryContrastText = ColorOrFallback(source.SecondaryContrastText, fallback.SecondaryContrastText),
            Tertiary = ColorOrFallback(source.Tertiary, fallback.Tertiary),
            TertiaryContrastText = ColorOrFallback(source.TertiaryContrastText, fallback.TertiaryContrastText),
            Info = ColorOrFallback(source.Info, fallback.Info),
            InfoContrastText = ColorOrFallback(source.InfoContrastText, fallback.InfoContrastText),
            Success = ColorOrFallback(source.Success, fallback.Success),
            SuccessContrastText = ColorOrFallback(source.SuccessContrastText, fallback.SuccessContrastText),
            Warning = ColorOrFallback(source.Warning, fallback.Warning),
            WarningContrastText = ColorOrFallback(source.WarningContrastText, fallback.WarningContrastText),
            Error = ColorOrFallback(source.Error, fallback.Error),
            ErrorContrastText = ColorOrFallback(source.ErrorContrastText, fallback.ErrorContrastText),
            Background = ColorOrFallback(source.Background, fallback.Background),
            Surface = ColorOrFallback(source.Surface, fallback.Surface),
            AppbarBackground = ColorOrFallback(source.AppbarBackground, fallback.AppbarBackground),
            AppbarText = ColorOrFallback(source.AppbarText, fallback.AppbarText),
            TextPrimary = ColorOrFallback(source.TextPrimary, fallback.TextPrimary),
            TextSecondary = ColorOrFallback(source.TextSecondary, fallback.TextSecondary),
            Divider = ColorOrFallback(source.Divider, fallback.Divider)
        };
    }

    private static ThemePalette NormalizePalette(ThemePalette palette)
        => new()
        {
            Primary = NormalizeColor(palette.Primary),
            PrimaryContrastText = NormalizeColor(palette.PrimaryContrastText),
            Secondary = NormalizeColor(palette.Secondary),
            SecondaryContrastText = NormalizeColor(palette.SecondaryContrastText),
            Tertiary = NormalizeColor(palette.Tertiary),
            TertiaryContrastText = NormalizeColor(palette.TertiaryContrastText),
            Info = NormalizeColor(palette.Info),
            InfoContrastText = NormalizeColor(palette.InfoContrastText),
            Success = NormalizeColor(palette.Success),
            SuccessContrastText = NormalizeColor(palette.SuccessContrastText),
            Warning = NormalizeColor(palette.Warning),
            WarningContrastText = NormalizeColor(palette.WarningContrastText),
            Error = NormalizeColor(palette.Error),
            ErrorContrastText = NormalizeColor(palette.ErrorContrastText),
            Background = NormalizeColor(palette.Background),
            Surface = NormalizeColor(palette.Surface),
            AppbarBackground = NormalizeColor(palette.AppbarBackground),
            AppbarText = NormalizeColor(palette.AppbarText),
            TextPrimary = NormalizeColor(palette.TextPrimary),
            TextSecondary = NormalizeColor(palette.TextSecondary),
            Divider = NormalizeColor(palette.Divider)
        };

    private static string ColorOrFallback(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string NormalizeColor(string value)
        => value.Trim().ToUpperInvariant();
}
