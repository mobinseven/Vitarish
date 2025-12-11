using Microsoft.Extensions.Logging.Abstractions;

namespace Vitarish.Instagram.Scraper.Tests;

public sealed class InstagramEndpointRouteBuilderExtensionsTests
{
    [Fact]
    public async Task HandleFetchAsync_WithValidDescriptor_ReturnsOkResult()
    {
        StubInstagramService service = new()
        {
            FetchResult = [new InstagramData { Id = "1", DataType = InstagramDataType.Posts, Url = "https://instagram.com/p/1" }],
        };

        var result = await InstagramEndpointRouteBuilderExtensions.HandleFetchAsync(
            new InstagramDataDescriptor { DirectUrls = new[] { "https://instagram.com/p/1" } },
            service,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        result.Result.Should().BeOfType<Ok<InstagramData[]>>();
        result.Result.As<Ok<InstagramData[]>>().Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleFetchAsync_ShouldReturnBadRequestOnValidationError()
    {
        StubInstagramService service = new()
        {
            FetchException = new ArgumentException("descriptor invalid"),
        };

        var result = await InstagramEndpointRouteBuilderExtensions.HandleFetchAsync(
            new InstagramDataDescriptor(),
            service,
            NullLoggerFactory.Instance,
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequest<string>>();
        result.Result.As<BadRequest<string>>().Value.Should().Contain("descriptor invalid");
    }

    private sealed class StubInstagramService : IInstagramScraperService
    {
        public InstagramData[] FetchResult { get; init; } = Array.Empty<InstagramData>();

        public InstagramRunInfo RunInfo { get; init; } = new InstagramRunInfo("run", "dataset", new RunsResponseSchema());

        public InstagramRawOutput RawOutput { get; init; } = new InstagramRawOutput(JsonDocument.Parse("{}").RootElement.Clone());

        public Exception? FetchException { get; init; }

        public Task<InstagramData[]> FetchAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default)
        {
            if (FetchException is not null)
            {
                throw FetchException;
            }

            return Task.FromResult(FetchResult);
        }

        public Task<InstagramRunInfo> StartRunAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default) => Task.FromResult(RunInfo);

        public Task<InstagramRawOutput> RunSyncAsync(InstagramDataDescriptor descriptor, CancellationToken cancellationToken = default) => Task.FromResult(RawOutput);
    }
}
