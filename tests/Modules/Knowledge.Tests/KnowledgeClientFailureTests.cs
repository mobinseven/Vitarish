using System.Net;
using FluentAssertions;
using Vitarish.Knowledge.Client;
using Vitarish.Knowledge.Contracts.Errors;
using Vitarish.Knowledge.Contracts.Requests;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeClientFailureTests
{
    public static TheoryData<HttpStatusCode, KnowledgeErrorCode> StatusMappings => new()
    {
        { HttpStatusCode.BadRequest, KnowledgeErrorCode.ValidationFailure },
        { HttpStatusCode.Unauthorized, KnowledgeErrorCode.AuthorizationFailure },
        { HttpStatusCode.Forbidden, KnowledgeErrorCode.Forbidden },
        { HttpStatusCode.NotFound, KnowledgeErrorCode.NotFound },
        { HttpStatusCode.Conflict, KnowledgeErrorCode.Conflict },
        { HttpStatusCode.UnsupportedMediaType, KnowledgeErrorCode.UnsupportedFileType },
        { HttpStatusCode.GatewayTimeout, KnowledgeErrorCode.Timeout },
        { HttpStatusCode.ServiceUnavailable, KnowledgeErrorCode.ProviderFailure },
        { HttpStatusCode.InternalServerError, KnowledgeErrorCode.UnexpectedFailure }
    };

    [Theory]
    [MemberData(nameof(StatusMappings))]
    public async Task AskAsync_MapsHttpStatusFailures(HttpStatusCode statusCode, KnowledgeErrorCode expected)
    {
        using HttpClient http = new(new StubHandler(_ => new HttpResponseMessage(statusCode) { Content = new StringContent("failed") })) { BaseAddress = new Uri("https://example.test") };
        KnowledgeHttpClient client = new(http);

        var result = await client.AskAsync(new AskKnowledgeRequest(Guid.NewGuid(), "question"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error!.Code.Should().Be(expected);
    }

    [Fact]
    public async Task AskAsync_MapsDeserializationFailure()
    {
        using HttpClient http = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") })) { BaseAddress = new Uri("https://example.test") };
        KnowledgeHttpClient client = new(http);

        var result = await client.AskAsync(new AskKnowledgeRequest(Guid.NewGuid(), "question"), CancellationToken.None);

        result.Error!.Code.Should().Be(KnowledgeErrorCode.DeserializationFailure);
    }

    [Fact]
    public async Task AskAsync_MapsCallerCancellation()
    {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        using HttpClient http = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))) { BaseAddress = new Uri("https://example.test") };
        KnowledgeHttpClient client = new(http);

        var result = await client.AskAsync(new AskKnowledgeRequest(Guid.NewGuid(), "question"), cts.Token);

        result.Error!.Code.Should().Be(KnowledgeErrorCode.Cancellation);
    }

    [Theory]
    [InlineData(KnowledgeErrorCode.FileUnavailable)]
    [InlineData(KnowledgeErrorCode.ParseFailure)]
    [InlineData(KnowledgeErrorCode.IndexingFailure)]
    [InlineData(KnowledgeErrorCode.RetrievalFailure)]
    public void ErrorTaxonomy_ContainsRequiredKnowledgeFailureCodes(KnowledgeErrorCode code) => Enum.GetValues<KnowledgeErrorCode>().Should().Contain(code);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(send(request));
        }
    }
}
