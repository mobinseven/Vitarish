using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Communications.Domain.Options;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

namespace Main.Tests;

// Sign-in codes run through Main's composition and SMS.ir's verify endpoint, so its response contract is proven here.
public sealed class SmsDotIrTemplatedSendTests
{
    [Fact]
    public async Task Verify_response_message_id_identifies_the_sent_code()
    {
        SmsDotIrProvider provider = ProviderReturning("""{"status":1,"message":"موفق","data":{"messageId":89545112,"cost":1.0}}""");

        var result = await provider.SendTemplatedSms("09120000000", "100000", new Dictionary<string, string> { ["Code"] = "123456" });

        result.MessageId.Should().Be("89545112");
        result.Cost.Should().Be(1.0m);
    }

    [Fact]
    public async Task Verify_response_without_a_positive_message_id_is_rejected()
    {
        SmsDotIrProvider provider = ProviderReturning("""{"status":1,"message":"موفق","data":{"messageId":0,"cost":1.0}}""");

        Func<Task> send = () => provider.SendTemplatedSms("09120000000", "100000", new Dictionary<string, string> { ["Code"] = "123456" });

        await send.Should().ThrowAsync<InvalidOperationException>();
    }

    private static SmsDotIrProvider ProviderReturning(string json)
    {
        Mock<IHttpClientFactory> factory = new();
        factory.Setup(item => item.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(new FixedResponseHandler(json)));
        return new SmsDotIrProvider(Options.Create(new ProvidersOptions()), factory.Object);
    }

    private sealed class FixedResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
