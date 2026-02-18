using Vitarish.Interfaces.Base.Common;
namespace Vitarish.Interfaces.Base.Plugins;

public sealed record FeedPlugin : PluginContext;

public interface IFeedPlugin : IPlugin<FeedPlugin>;
