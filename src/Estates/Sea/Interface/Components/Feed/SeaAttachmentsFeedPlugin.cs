using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Sea.Interface.Components.Feed;

internal sealed class SeaAttachmentsFeedPlugin : IFeedPlugin
{
    public const string PluginId = "sea.attachments.recent";
    public string Id => PluginId;

    public RenderFragment Render(FeedPlugin context) => builder =>
    {
        builder.OpenComponent<SeaAttachmentsFeedSource>(0);
        builder.AddComponentParameter(1, nameof(SeaAttachmentsFeedSource.Context), context);
        builder.CloseComponent();
    };
}
