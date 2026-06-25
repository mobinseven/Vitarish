using System.Net;
using FluentAssertions;
using Vitarish.Communications.Client;
using Vitarish.Communications.Contracts.Errors;
using Vitarish.Communications.Contracts.Requests;
using Xunit;
namespace Communications.Tests.Client;
public sealed class CommunicationsClientFailureTests
{
    public static TheoryData<HttpStatusCode, CommunicationsErrorCode> StatusMappings => new(){ {HttpStatusCode.BadRequest,CommunicationsErrorCode.ValidationFailure},{HttpStatusCode.Unauthorized,CommunicationsErrorCode.Unauthorized},{HttpStatusCode.Forbidden,CommunicationsErrorCode.Forbidden},{HttpStatusCode.NotFound,CommunicationsErrorCode.NotFound},{HttpStatusCode.Conflict,CommunicationsErrorCode.Duplicate},{HttpStatusCode.ServiceUnavailable,CommunicationsErrorCode.ProviderUnavailable},{HttpStatusCode.GatewayTimeout,CommunicationsErrorCode.Timeout},{HttpStatusCode.InternalServerError,CommunicationsErrorCode.UnexpectedFailure} };
    [Theory][MemberData(nameof(StatusMappings))] public async Task SendSmsAsync_MapsExpectedFailures(HttpStatusCode statusCode, CommunicationsErrorCode expected){ using HttpClient httpClient=new(new StubHandler(statusCode,"failed")){BaseAddress=new Uri("https://example.test")}; CommunicationsClient client=new(httpClient); var result=await client.SendSmsAsync(new SendSmsRequest("+15555550123","body")); result.IsSuccess.Should().BeFalse(); result.Error!.Code.Should().Be(expected); }
    [Fact] public async Task GetSmsDeliveryStateAsync_MapsDeserializationFailure(){ using HttpClient httpClient=new(new StubHandler(HttpStatusCode.OK,"not json")){BaseAddress=new Uri("https://example.test")}; CommunicationsClient client=new(httpClient); var result=await client.GetSmsDeliveryStateAsync(123); result.IsSuccess.Should().BeFalse(); result.Error!.Code.Should().Be(CommunicationsErrorCode.DeserializationFailure); }
    private sealed class StubHandler(HttpStatusCode statusCode,string content):HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(new HttpResponseMessage(statusCode){Content=new StringContent(content)}); }
}
