using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class FeedWidget : ComponentBase
{
    [Inject] private IEnumerable<IFeedPlugin> FeedPlugins { get; set; } = [];
    [Parameter] public IReadOnlyDictionary<string, object?> Configuration { get; set; } = new Dictionary<string, object?>();
    [Parameter] public int Take { get; set; } = 5;

    protected IReadOnlyList<IFeedPlugin> ConfiguredPlugins { get; private set; } = [];
    protected FeedPlugin Context { get; private set; } = new(new Dictionary<string, object?>());

    protected override void OnParametersSet()
    {
        IFeedPlugin[] allPlugins = [.. FeedPlugins];
        IReadOnlyList<string> sourceIds = FeedPluginConfiguration.GetConfiguredSourceIds(Configuration);
        Context = new FeedPlugin(Configuration, Take);

        if (sourceIds.Count == 0 || allPlugins.Length == 0)
        {
            ConfiguredPlugins = [];
            return;
        }

        Dictionary<string, IFeedPlugin> pluginMap = allPlugins
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        ConfiguredPlugins = [.. sourceIds
            .Where(pluginMap.ContainsKey)
            .Select(x => pluginMap[x])];
    }
}
