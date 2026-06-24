using FluentAssertions;
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

    private sealed class StubHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }
}
