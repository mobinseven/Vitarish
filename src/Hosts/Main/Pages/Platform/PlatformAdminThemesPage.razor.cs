using Vitarish.Platform.Application.Abstractions;
using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Models;

namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminThemesPage : PlatformAdminPageBase
{
    [Inject] private IPlatformThemeQuery ThemeQuery { get; set; } = default!;
    [Inject] private IPlatformThemeCommand ThemeCommand { get; set; } = default!;

    private IReadOnlyList<ThemeListItem> Themes { get; set; } = [];
    private string ActiveThemeName => Themes.FirstOrDefault(e => e.IsActive)?.Name ?? Localizer["None"];
    private string ThemeSummary => $"{Localizer["Active theme"]}: {ActiveThemeName}";

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync() => Themes = await ThemeQuery.ListThemesAsync();

    private async Task Activate(Guid themeId)
    {
        PlatformCommandResult result = await ThemeCommand.SetActiveThemeAsync(themeId);
        if (!result.Succeeded)
        {
            _ = Snackbar.Add(result.Error ?? Localizer["Error"], Severity.Error);
            return;
        }

        _ = Snackbar.Add(Localizer["Theme activated"], Severity.Success);
        await ReloadAsync();
    }

    private async Task Delete(Guid themeId)
    {
        PlatformCommandResult result = await ThemeCommand.DeleteInactiveThemeAsync(themeId);
        if (!result.Succeeded)
        {
            _ = Snackbar.Add(result.Error ?? Localizer["Active theme cannot be deleted"], Severity.Error);
            return;
        }

        _ = Snackbar.Add(Localizer["Theme deleted"], Severity.Success);
        await ReloadAsync();
    }

    private string AvailabilityLabel(ThemeListItem theme) => Localizer["both"];

    private static IReadOnlyList<string> Preview(ThemePalette palette)
        => [palette.Primary, palette.Secondary, palette.Tertiary, palette.Info, palette.Success, palette.Warning, palette.Error, palette.Background, palette.Surface, palette.TextPrimary];
}
