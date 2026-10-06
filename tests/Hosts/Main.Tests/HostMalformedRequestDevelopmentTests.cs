using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Vitarish.Main.Tests;

public sealed class HostMalformedRequestDevelopmentTests : IAsyncLifetime
{
    private const int MaximumBodyBytes = 1_024;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        (_app, _client) = await HostMalformedRequestTests.StartProbeAppAsync(Environments.Development);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Invalid_json_maps_to_a_problem_4xx_in_development()
    {
        HttpResponseMessage response = await _client.PostAsync(
            "/probe/json",
            new StringContent("{ \"count\": ", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Oversized_body_maps_to_413_in_development()
    {
        string payload = $$"""{ "name": "{{new string('x', MaximumBodyBytes * 4)}}" }""";
        HttpResponseMessage response = await _client.PostAsync(
            "/probe/json",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
