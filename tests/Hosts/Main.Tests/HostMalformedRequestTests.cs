using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vitarish.Hosting;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using PartiesTenantResolver = Vitarish.Parties.Application.Abstractions.ITenantResolver;

namespace Vitarish.Main.Tests;

public sealed class HostMalformedRequestTests : IAsyncLifetime
{
    private const long MaximumBodyBytes = 1_024;
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        (_app, _client) = await StartProbeAppAsync(Environments.Production);
    }

    internal static async Task<(WebApplication App, HttpClient Client)> StartProbeAppAsync(string environmentName)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = MaximumBodyBytes);
        _ = builder.AddHostRequestExceptionHandling();
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddDbContext<PartiesDbContext>(options => options.UseInMemoryDatabase("host-malformed-request-tenant"));
        _ = builder.Services.AddSingleton(Moq.Mock.Of<MediatR.IPublisher>());
        _ = builder.Services.AddScoped<PartiesTenantResolver, TenantResolver>();
        _ = builder.Services.AddMultiTenant<TenantInfo>().WithEchoStore().WithDelegateStrategy(async context =>
        {
            if (context is not HttpContext http) return null;
            PartiesTenantResolver tenants = http.RequestServices.GetRequiredService<PartiesTenantResolver>();
            Tenant? tenant = await tenants.ResolveAsync(http, http.RequestAborted);
            return tenant?.Id.ToString();
        });

        WebApplication app = builder.Build();
        _ = app.UseExceptionHandler();
        _ = app.UseMultiTenant();
        _ = app.MapGet("/probe/values/{id:int}", (int id, int? take) => Results.Ok(new { id, take }));
        _ = app.MapPost("/probe/json", (ProbeBody body) => Results.Ok(new { body.Count, body.Name }));
        _ = app.MapPost("/probe/upload", (IFormFile file) => Results.Ok(new { file.Length })).DisableAntiforgery();
        _ = app.MapGet("/probe/tenant", (HttpContext context) => Results.Ok(new
        {
            tenant = context.RequestServices.GetRequiredService<IMultiTenantContextAccessor>().MultiTenantContext?.TenantInfo?.Id
        }));
        _ = app.MapGet("/probe/bug", (HttpContext _) => throw new InvalidOperationException("probe-bug-secret"));
        await app.StartAsync();

        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return (app, new HttpClient { BaseAddress = new Uri(address) });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Invalid_json_maps_to_400_problem_details_without_a_stack_trace()
    {
        HttpResponseMessage response = await PostJsonAsync("{ \"count\": ");

        await AssertClientErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Wrong_json_type_maps_to_400_problem_details()
    {
        HttpResponseMessage response = await PostJsonAsync("""{ "count": "not-an-integer", "name": "probe" }""");

        await AssertClientErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_required_body_maps_to_400_problem_details()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/probe/json")
        {
            Content = new ByteArrayContent([])
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        HttpResponseMessage response = await _client.SendAsync(request);

        await AssertClientErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unbindable_query_value_maps_to_400_problem_details()
    {
        HttpResponseMessage response = await _client.GetAsync("/probe/values/7?take=not-an-integer");

        await AssertClientErrorAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Oversized_body_maps_to_413_problem_details()
    {
        string payload = $$"""{ "name": "{{new string('x', 4_096)}}" }""";
        HttpResponseMessage response = await PostJsonAsync(payload);

        await AssertClientErrorAsync(response, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Unsupported_content_type_maps_to_415_problem_details()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/probe/json")
        {
            Content = new StringContent("""{ "count": 1 }""", Encoding.UTF8, "text/plain")
        };
        HttpResponseMessage response = await _client.SendAsync(request);

        await AssertClientErrorAsync(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Malformed_multipart_upload_maps_to_a_4xx_problem_details()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/probe/upload")
        {
            Content = new StringContent("not-a-multipart-body", Encoding.UTF8, "multipart/form-data")
        };
        HttpResponseMessage response = await _client.SendAsync(request);

        await AssertClientErrorAsync(response, HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Invalid_tenant_header_does_not_produce_a_server_error()
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/probe/tenant");
        request.Headers.Add(TenantResolver.TenantHostHeaderName, "invalid host:::not-a-host");

        HttpResponseMessage response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_genuine_server_bug_stays_a_500_without_leaking_details_in_production()
    {
        HttpResponseMessage response = await _client.GetAsync("/probe/bug");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("probe-bug-secret");
        body.Should().NotContain("System.");
    }

    private Task<HttpResponseMessage> PostJsonAsync(string json) =>
        _client.PostAsync("/probe/json", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task AssertClientErrorAsync(HttpResponseMessage response, params HttpStatusCode[] expectedStatusCodes)
    {
        expectedStatusCodes.Should().Contain(response.StatusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        string body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("System.");
        body.Should().NotContain("   at ");
    }

    public sealed record ProbeBody(int Count, string? Name);
}
