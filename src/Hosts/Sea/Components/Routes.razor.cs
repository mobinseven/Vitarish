using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Vitarish.Clients.Interface.Sea.Abstractions;
using Vitarish.Clients.Interface.Sea.Components.UsageConfigurations;
using Vitarish.Clients.Interface.Services;

namespace Vitarish.Sea.Components
{
    public partial class Routes : IDisposable
    {
        [Inject] protected ISeaAccessService AccessService { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState>? AuthStateTask { get; set; }
        [Inject] private ILogger<Routes> Logger { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IUsageConfigurationClient UsageConfigurationService { get; set; } = default!;
        [Inject] private IThemeService ThemeService { get; set; } = default!;

        private Task? _themeInitializationTask;

        // Use the AAAS client ID for authorization
        private readonly string? Resource;

        private async Task OnNavigating(NavigationContext context)
        {
            if (AccessService.User is null)
            {
                await AccessService.InitializeAsync().ConfigureAwait(false);
            }

            await EnsureThemeInitializedAsync().ConfigureAwait(false);
        }

        private Task EnsureThemeInitializedAsync()
            => _themeInitializationTask ??= InitializeThemeAsync();

        private async Task InitializeThemeAsync()
        {
            try
            {
                UsageConfigurationDto? configuration = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
                UsageConfigurationMetadata metadata = UsageConfigurationMetadata.FromJson(configuration?.Metadata);
                if (!string.IsNullOrWhiteSpace(metadata.ThemeSettingsJson))
                {
                    ThemeService.LoadThemeSettingsJson(metadata.ThemeSettingsJson);
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to initialize theme settings from usage configuration.");
            }
        }

        public void Dispose()
        {
            // No-op, tenant resolution is handled by middleware
        }
    }
}
