using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Sea.Interface.Components.Feed;

internal sealed class SeaProjectsFeedPlugin : IFeedPlugin
{
    public const string PluginId = "sea.projects.recent";
    public string Id => PluginId;

    public RenderFragment Render(FeedPlugin context) => builder =>
    {
        builder.OpenComponent<SeaProjectsFeedSource>(0);
        builder.AddComponentParameter(1, nameof(SeaProjectsFeedSource.Context), context);
        builder.CloseComponent();
    };
}
