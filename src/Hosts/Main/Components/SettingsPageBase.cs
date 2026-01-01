namespace Vitarish.Main.Components
{
    public abstract class SettingsPageBase : ComponentBase
    {
        [Inject]
        public IStringLocalizer Localizer { get; set; } = default!;

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
