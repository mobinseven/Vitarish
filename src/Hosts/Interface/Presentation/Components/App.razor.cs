namespace Vitarish.Interface.Presentation.Components;

public partial class App
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;
    private IComponentRenderMode? RenderModeForPage => HttpContext.Request.Path.StartsWithSegments("/Account")
        ? null : InteractiveServer;

    private readonly MudTheme theme = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = Colors.Teal.Darken1,
            Secondary = Colors.Teal.Darken4,
            WarningDarken = Colors.Gray.Darken1,
            Warning = Colors.Gray.Lighten1,
            WarningLighten = Colors.Gray.Lighten2,
            WarningContrastText = Colors.Shades.White,
            AppbarBackground = Colors.Teal.Accent4,
        },
        PaletteDark = new PaletteDark()
        {
            Primary = Colors.Teal.Darken1,
            Secondary = Colors.Teal.Darken4,
            Background = Colors.Gray.Darken4,
            AppbarBackground = Colors.Teal.Accent4,
            DrawerBackground = Colors.Gray.Darken3,
            Surface = Colors.Gray.Darken4,
        },
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "0",
        },
        Typography = new Typography()
        {
            Default = new DefaultTypography()
            {
                FontFamily = new[] { "Ahang", "SegoeUI", "sans-serif" },//
                FontWeight = "100",
            }
        }
    };

}
