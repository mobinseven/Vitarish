using Microsoft.Extensions.Logging.Abstractions;

namespace Vitarish.Instagram.Scraper.Tests;

public sealed class InstagramScraperServiceTests
{
    [Fact]
    public async Task FetchAsync_WithValidUrls_MapsInputAndFiltersResults()
    {
        InputSchema? capturedInput = null;
        var itemsJson = "[{\"id\":\"1\",\"url\":\"https://instagram.com/p/1\",\"ownerUsername\":\"john\",\"caption\":\"hello\",\"timestamp\":1700000000,\"likesCount\":5,\"commentsCount\":2},{\"id\":\"2\",\"url\":\"https://instagram.com/p/2\",\"timestamp\":1500000000}]";
        DatasetItemsResponse dataset;
        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(itemsJson)))
        {
            dataset = await DatasetItemsResponse.FromStreamAsync(ms, CancellationToken.None);
        }

        Mock<IInstagramScraperApiClient> client = new();
        client.Setup(c => c.RunScraperAndGetDatasetItemsAsync(It.IsAny<InputSchema>(), It.IsAny<CancellationToken>()))
            .Callback<InputSchema, CancellationToken>((input, _) => capturedInput = input)
            .ReturnsAsync(dataset);

        InstagramScraperService sut = new(client.Object, NullLogger<InstagramScraperService>.Instance);

        InstagramDataDescriptor descriptor = new()
        {
            DirectUrls = new[] { "https://instagram.com/profile" },
            DataType = InstagramDataType.Comments,
            MaxResultsPerUrl = 25,
            FromPublishedAt = DateTimeOffset.FromUnixTimeSeconds(1_600_000_000),
            ToPublishedAt = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000),
            ExtraOptions = new Dictionary<string, object?> { { "proxyConfiguration", new { useApifyProxy = true } } },
        };

        InstagramData[] result = await sut.FetchAsync(descriptor);

        result.Should().ContainSingle();
        result[0].Id.Should().Be("1");
        result[0].DataType.Should().Be(InstagramDataType.Comments);
        result[0].AuthorUsername.Should().Be("john");
        result[0].LikeCount.Should().Be(5);
        capturedInput.Should().NotBeNull();
        capturedInput!.ResultsType.Should().Be(InputSchema_resultsType.Comments);
        capturedInput.ResultsLimit.Should().Be(25);
        capturedInput.OnlyPostsNewerThan.Should().NotBeNull();
        capturedInput.AdditionalData.Should().ContainKey("proxyConfiguration");
    }

    [Fact]
    public async Task StartRunAsync_WithValidDescriptor_ReturnsRunInfo()
    {
        Mock<IInstagramScraperApiClient> client = new();
        RunInfoResponse runInfo = new(new RunsResponseSchema
        {
            Data = new RunsResponseSchema_data
            {
                Id = "run-123",
                DefaultDatasetId = "dataset-1",
            },
        });

        client.Setup(c => c.StartScraperRunAsync(It.IsAny<InputSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runInfo);

        InstagramScraperService sut = new(client.Object, NullLogger<InstagramScraperService>.Instance);

        InstagramRunInfo result = await sut.StartRunAsync(new InstagramDataDescriptor
        {
            DirectUrls = new[] { "https://instagram.com/p/abc" },
        });

        result.RunId.Should().Be("run-123");
        result.DefaultDatasetId.Should().Be("dataset-1");
        result.Raw.Data!.Id.Should().Be("run-123");
    }

    [Fact]
    public async Task RunSyncAsync_WithSearchQuery_WrapsRawOutput()
    {
        Mock<IInstagramScraperApiClient> client = new();
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"ok\":true}"));
        var raw = await KeyValueStoreOutput.FromStreamAsync(ms, CancellationToken.None);

        client.Setup(c => c.RunScraperSyncAsync(It.IsAny<InputSchema>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);

        InstagramScraperService sut = new(client.Object, NullLogger<InstagramScraperService>.Instance);

        InstagramRawOutput result = await sut.RunSyncAsync(new InstagramDataDescriptor
        {
            SearchQuery = "sample",
        });

        result.Data.GetProperty("ok").GetBoolean().Should().BeTrue();
    }
}
