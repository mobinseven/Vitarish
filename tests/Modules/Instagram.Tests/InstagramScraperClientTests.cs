namespace Instagram.Scraper.Tests;

public class InstagramScraperClientTests
{
    [Fact]
    public async Task RunScraperAndGetDatasetItemsAsync_ShouldAttachTokenAndParseItems()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":1},{\"id\":2}]", Encoding.UTF8, "application/json"),
        });

        using HttpClient httpClient = handler.CreateClient();
        var client = new InstagramScraperClient("test-token", httpClient);

        var input = new InputSchema
        {
            DirectUrls = new List<string> { "https://instagram.com/p/abc" },
        };

        DatasetItemsResponse response = await client.RunScraperAndGetDatasetItemsAsync(input);

        response.Items.Should().HaveCount(2);
        response.Raw.ValueKind.Should().Be(JsonValueKind.Array);
        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.Query.Should().Contain("token=test-token");
    }

    [Fact]
    public async Task RunScraperSyncAsync_ShouldRetryOnTransientFailure()
    {
        var handler = new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json"),
            });

        using HttpClient httpClient = handler.CreateClient();
        var client = new InstagramScraperClient("retry-token", httpClient, retryOptions: new RetryOptions(MaxAttempts: 2, InitialDelay: TimeSpan.FromMilliseconds(1), MaxDelay: TimeSpan.FromMilliseconds(2)));

        KeyValueStoreOutput result = await client.RunScraperSyncAsync(new InputSchema());

        handler.Requests.Should().HaveCount(2);
        result.Data.GetProperty("ok").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task StartScraperRunAsync_ShouldThrowWhenResponseMissing()
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        });

        using HttpClient httpClient = handler.CreateClient();
        var client = new InstagramScraperClient("missing-response", httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.StartScraperRunAsync(new InputSchema()));
    }
}
