using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Main.Mock.Services;

internal sealed class MockRecentProjectsFeedPlugin : IFeedPlugin
{
    public string Id => "sea.projects.recent";

    public RenderFragment Render(FeedPlugin context) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "Recent projects: Project A, Project B");
        builder.CloseElement();
    };
}

internal sealed class MockRecentAttachmentsFeedPlugin : IFeedPlugin
{
    public string Id => "sea.attachments.recent";

    public RenderFragment Render(FeedPlugin context) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddContent(1, "Recent attachments: report.pdf, photo.png");
        builder.CloseElement();
    };
}
