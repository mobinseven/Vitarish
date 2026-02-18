using Vitarish.Modules.Base;

namespace Vitarish.Modules.Feed.Tests;

/// <summary>
/// Unit tests for the <see cref="FeedService"/>.
/// </summary>
public sealed class FeedServiceTests
{
    [Fact]
    public void GetAvailableSources_ReturnsAllRegisteredSources()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        var source1 = new Mock<IFeedDataSource>();
        source1.Setup(s => s.SourceId).Returns("source1");
        source1.Setup(s => s.Priority).Returns(10);

        var source2 = new Mock<IFeedDataSource>();
        source2.Setup(s => s.SourceId).Returns("source2");
        source2.Setup(s => s.Priority).Returns(20);

        var sources = new[] { source1.Object, source2.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        // Act
        var result = feedService.GetAvailableSources();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(source1.Object);
        result.Should().ContainEquivalentOf(source2.Object);
    }

    [Fact]
    public void GetAvailableSources_OrdersByPriority()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        var source1 = new Mock<IFeedDataSource>();
        source1.Setup(s => s.SourceId).Returns("source1");
        source1.Setup(s => s.Priority).Returns(20);

        var source2 = new Mock<IFeedDataSource>();
        source2.Setup(s => s.SourceId).Returns("source2");
        source2.Setup(s => s.Priority).Returns(10);

        var sources = new[] { source1.Object, source2.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        // Act
        var result = feedService.GetAvailableSources();

        // Assert
        result.Should().HaveCount(2);
        result[0].SourceId.Should().Be("source2"); // Lower priority comes first
        result[1].SourceId.Should().Be("source1");
    }

    [Fact]
    public async Task GetFeedAsync_AggregatesFromMultipleSources()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        
        var item1 = new FeedItem
        {
            Id = Guid.NewGuid(),
            SourceId = "source1",
            SourceName = "Source 1",
            Title = "Item 1",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            ItemType = "type1"
        };

        var item2 = new FeedItem
        {
            Id = Guid.NewGuid(),
            SourceId = "source2",
            SourceName = "Source 2",
            Title = "Item 2",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            ItemType = "type2"
        };

        var source1 = new Mock<IFeedDataSource>();
        source1.Setup(s => s.SourceId).Returns("source1");
        source1.Setup(s => s.Priority).Returns(10);
        source1.Setup(s => s.EnabledByDefault).Returns(true);
        source1.Setup(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { item1 });

        var source2 = new Mock<IFeedDataSource>();
        source2.Setup(s => s.SourceId).Returns("source2");
        source2.Setup(s => s.Priority).Returns(20);
        source2.Setup(s => s.EnabledByDefault).Returns(true);
        source2.Setup(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { item2 });

        var sources = new[] { source1.Object, source2.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        var options = new FeedQueryOptions { Limit = 10 };

        // Act
        var result = await feedService.GetFeedAsync(options);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(item1);
        result.Should().Contain(item2);
    }

    [Fact]
    public async Task GetFeedAsync_SortsByTimestampDescending()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        
        var olderItem = new FeedItem
        {
            Id = Guid.NewGuid(),
            SourceId = "source1",
            SourceName = "Source 1",
            Title = "Older Item",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            ItemType = "type1"
        };

        var newerItem = new FeedItem
        {
            Id = Guid.NewGuid(),
            SourceId = "source1",
            SourceName = "Source 1",
            Title = "Newer Item",
            Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
            ItemType = "type1"
        };

        var source = new Mock<IFeedDataSource>();
        source.Setup(s => s.SourceId).Returns("source1");
        source.Setup(s => s.Priority).Returns(10);
        source.Setup(s => s.EnabledByDefault).Returns(true);
        source.Setup(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem> { olderItem, newerItem });

        var sources = new[] { source.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        var options = new FeedQueryOptions { Limit = 10 };

        // Act
        var result = await feedService.GetFeedAsync(options);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(newerItem); // Newer items first
        result[1].Should().Be(olderItem);
    }

    [Fact]
    public async Task GetFeedAsync_RespectsLimitOption()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        
        var items = Enumerable.Range(1, 10)
            .Select(i => new FeedItem
            {
                Id = Guid.NewGuid(),
                SourceId = "source1",
                SourceName = "Source 1",
                Title = $"Item {i}",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-i),
                ItemType = "type1"
            })
            .ToList();

        var source = new Mock<IFeedDataSource>();
        source.Setup(s => s.SourceId).Returns("source1");
        source.Setup(s => s.Priority).Returns(10);
        source.Setup(s => s.EnabledByDefault).Returns(true);
        source.Setup(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var sources = new[] { source.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        var options = new FeedQueryOptions { Limit = 5 };

        // Act
        var result = await feedService.GetFeedAsync(options);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void GetConfiguration_ReturnsDefaultWhenNoMetadata()
    {
        // Arrange
        var mockTenantService = new Mock<ITenantService>();
        mockTenantService.Setup(s => s.Metadata).Returns((Vitarish.Entities.Server.Models.Metadata?)null);

        var feedService = new FeedService([], mockTenantService.Object);

        // Act
        var result = feedService.GetConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.DefaultPageSize.Should().Be(50);
        result.MaxPageSize.Should().Be(100);
        result.SortOrder.Should().Be("timestamp_desc");
    }

    [Fact]
    public async Task GetFeedAsync_FiltersSourcesByConfiguration()
    {
        // Arrange
        var mockMetadata = new Vitarish.Entities.Server.Models.Metadata();
        var config = new FeedConfiguration
        {
            EnabledSourceIds = new[] { "source1" }.ToList()
        };
        mockMetadata["feed.configuration"] = System.Text.Json.JsonSerializer.Serialize(config);

        var mockTenantService = new Mock<ITenantService>();
        mockTenantService.Setup(s => s.Metadata).Returns(mockMetadata);

        var source1 = new Mock<IFeedDataSource>();
        source1.Setup(s => s.SourceId).Returns("source1");
        source1.Setup(s => s.Priority).Returns(10);
        source1.Setup(s => s.EnabledByDefault).Returns(true);
        source1.Setup(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeedItem>());

        var source2 = new Mock<IFeedDataSource>();
        source2.Setup(s => s.SourceId).Returns("source2");
        source2.Setup(s => s.Priority).Returns(20);
        source2.Setup(s => s.EnabledByDefault).Returns(true);

        var sources = new[] { source1.Object, source2.Object };
        var feedService = new FeedService(sources, mockTenantService.Object);

        var options = new FeedQueryOptions { Limit = 10 };

        // Act
        await feedService.GetFeedAsync(options);

        // Assert - only source1 should be called
        source1.Verify(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        source2.Verify(s => s.GetFeedItemsAsync(It.IsAny<FeedQueryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
