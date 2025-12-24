using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Sea.Abstractions;
using Vitarish.Clients.Interface.Sea.Components.UsageConfigurations;
using Vitarish.Clients.Interface.Services;

namespace Vitarish.Sea.Components
{
    public partial class Routes : IDisposable
    {
        [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
        [Inject] protected IIdentityService IdentityService { get; set; } = default!;
        [Inject] protected ISeaUsersService AccessService { get; set; } = default!;
        [Inject] private ILogger<Routes> Logger { get; set; } = default!;
        [Inject] private IUsageConfigurationClient UsageConfigurationService { get; set; } = default!;
        [Inject] private IThemeService ThemeService { get; set; } = default!;

        private Task? _themeInitializationTask;
        private async Task OnNavigating(NavigationContext context)
        {
            if (AccessService.User is null)
            {
                IdentityService.User ??= HttpContextAccessor.HttpContext?.User;
                await AccessService.Initialize(["User"]);
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
