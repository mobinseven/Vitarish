using System.Text.Json;
using Vitarish.Interfaces.Base.Plugins;

namespace Vitarish.Interfaces.Base.Tests.Plugins;

public class FeedPluginConfigurationTests
{
    [Fact]
    public void GetConfiguredSourceIds_WhenMetadataContainsStringArray_ReturnsSourceIds()
    {
        Dictionary<string, object?> metadata = new()
        {
            [FeedPluginConfiguration.SourceIdsMetadataKey] = new[] { "sea.projects.recent", "sea.attachments.recent" }
        };

        IReadOnlyList<string> result = FeedPluginConfiguration.GetConfiguredSourceIds(metadata);

        result.Should().Equal("sea.projects.recent", "sea.attachments.recent");
    }

    [Fact]
    public void GetConfiguredSourceIds_WhenMetadataContainsJsonArray_ReturnsSourceIds()
    {
        using JsonDocument jsonDocument = JsonDocument.Parse("""["sea.projects.recent","sea.attachments.recent"]""");
        Dictionary<string, object?> metadata = new()
        {
            [FeedPluginConfiguration.SourceIdsMetadataKey] = jsonDocument.RootElement.Clone()
        };

        IReadOnlyList<string> result = FeedPluginConfiguration.GetConfiguredSourceIds(metadata);

        result.Should().Equal("sea.projects.recent", "sea.attachments.recent");
    }

    [Fact]
    public void GetConfiguredSourceIds_WhenMetadataMissing_ReturnsEmpty()
    {
        IReadOnlyList<string> result = FeedPluginConfiguration.GetConfiguredSourceIds(new Dictionary<string, object?>());

        result.Should().BeEmpty();
    }
}
