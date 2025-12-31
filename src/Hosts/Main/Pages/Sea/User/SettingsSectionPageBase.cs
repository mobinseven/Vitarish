using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Vitarish.Sea.Interface.Abstractions;

namespace Vitarish.Main.Pages.Sea.User
{
    public abstract class SettingsSectionPageBase : ComponentBase
    {
        [Inject]
        public IStringLocalizer Localizer { get; set; } = default!;

        [Inject]
        public ISeaUsersService AccessService { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        protected virtual string BasePath => "/settings";

        protected IReadOnlyList<BreadcrumbItem> BuildBreadcrumbs(string? currentSection = null)
        {
            List<BreadcrumbItem> items =
            [
                new(Localizer["Settings"], BasePath, disabled: currentSection is null)
            ];

            if (!string.IsNullOrWhiteSpace(currentSection))
            {
                items.Add(new BreadcrumbItem(currentSection, href: null, disabled: true));
            }

            return items;
        }
    }
}
