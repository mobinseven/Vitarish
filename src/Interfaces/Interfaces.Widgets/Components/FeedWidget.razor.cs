using Microsoft.AspNetCore.Components;
using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Interfaces.Widgets.Components;

public partial class FeedWidget
{
    [Inject] private IEnumerable<IFeedPlugin> FeedPlugins { get; set; } = [];
}
