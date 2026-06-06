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
            .Select(e => new ThemeListItem(e.Id, e.Name, ToDto(e.LightPalette), ToDto(e.DarkPalette), selection != null && e.Id == selection.ActiveThemeId))
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

        DateTimeOffset now = timeProvider.GetUtcNow();
        Theme theme = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            LightPalette = ToEntity(model.LightPalette),
            DarkPalette = ToEntity(model.DarkPalette)
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
        theme.LightPalette = ToEntity(model.LightPalette);
        theme.DarkPalette = ToEntity(model.DarkPalette);
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
