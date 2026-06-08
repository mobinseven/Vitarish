namespace Vitarish.Main.Components;

public partial class App
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;
    private IComponentRenderMode? RenderModeForPage => HttpContext.Request.Path.StartsWithSegments("/Account")
        ? null : InteractiveServer;
}

