using FluentAssertions;
using System.Globalization;
using System.Net;
using Vitarish.Feed.Client;
using Vitarish.Feed.Contracts;
using Xunit;

namespace Vitarish.Feed.Tests;

public sealed class FeedHttpClientFailureTests
{
    [Fact]
    public async Task QueryFeedItemsAsync_MapsValidationFailure()
    {
        using HttpClient httpClient = new(new StubHandler(HttpStatusCode.BadRequest, """
            {"code":"validation","message":"Invalid feed query.","validationErrors":{"limit":["Limit is invalid."]}}
            """)) { BaseAddress = new Uri("https://example.test") };
        FeedHttpClient client = new(httpClient);

        FeedClientResult<IReadOnlyList<FeedItemResponse>> result = await client.QueryFeedItemsAsync(new FeedQueryParameters(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Kind.Should().Be(FeedClientFailureKind.Validation);
        result.Failure.ValidationErrors.Should().ContainKey("limit");
    }

    [Fact]
    public async Task QueryFeedSourcesAsync_MapsUnexpectedServerFailureWithoutInternalDetails()
    {
        using HttpClient httpClient = new(new StubHandler(HttpStatusCode.InternalServerError, "not-json")) { BaseAddress = new Uri("https://example.test") };
        FeedHttpClient client = new(httpClient);

        FeedClientResult<IReadOnlyList<FeedSourceResponse>> result = await client.QueryFeedSourcesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Kind.Should().Be(FeedClientFailureKind.UnexpectedServerFailure);
        result.Failure.Message.Should().Be("Feed API returned an unreadable error response.");
    }

    [Fact]
    public async Task QueryFeedItemsAsync_UsesInvariantCultureForQueryParameters()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        CapturingHandler handler = new(HttpStatusCode.OK, "[]");
        using HttpClient httpClient = new(handler) { BaseAddress = new Uri("https://example.test") };
        FeedHttpClient client = new(httpClient);

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = new CultureInfo("ar-SA");

            _ = await client.QueryFeedItemsAsync(new FeedQueryParameters
            {
                Limit = 12,
                Skip = 3,
                After = new DateTime(2026, 06, 24, 10, 15, 30, DateTimeKind.Utc),
                Before = new DateTime(2026, 06, 25, 11, 16, 31, DateTimeKind.Utc)
            }, CancellationToken.None);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        handler.RequestUri.Should().NotBeNull();
        handler.RequestUri!.Query.Should().Contain("limit=12");
        handler.RequestUri.Query.Should().Contain("skip=3");
        handler.RequestUri.Query.Should().Contain("after=2026-06-24T10%3A15%3A30.0000000Z");
        handler.RequestUri.Query.Should().Contain("before=2026-06-25T11%3A16%3A31.0000000Z");
    }

    [Fact]
    public async Task QueryFeedSourcesAsync_MapsNonJsonSuccessResponseToDeserializationFailure()
    {
        using HttpClient httpClient = new(new StubHandler(HttpStatusCode.OK, "<html>not-json</html>")) { BaseAddress = new Uri("https://example.test") };
        FeedHttpClient client = new(httpClient);

        FeedClientResult<IReadOnlyList<FeedSourceResponse>> result = await client.QueryFeedSourcesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Kind.Should().Be(FeedClientFailureKind.Deserialization);
    }

    [Fact]
    public async Task QueryFeedSourcesAsync_MapsTimeoutExceptionToTimeoutFailure()
    {
        using HttpClient httpClient = new(new ThrowingHandler(new TimeoutException("request timed out"))) { BaseAddress = new Uri("https://example.test") };
        FeedHttpClient client = new(httpClient);

        FeedClientResult<IReadOnlyList<FeedSourceResponse>> result = await client.QueryFeedSourcesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Kind.Should().Be(FeedClientFailureKind.Timeout);
        result.Failure.Message.Should().Be("Feed request timed out.");
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
