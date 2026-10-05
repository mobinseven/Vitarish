using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Hosting.Infrastructure.Pipeline;

namespace Vitarish.Main.Tests;

public sealed class HostRequestExceptionHandlerTests
{
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status413RequestEntityTooLarge)]
    [InlineData(StatusCodes.Status415UnsupportedMediaType)]
    public async Task Bad_http_request_failures_map_to_their_problem_status_without_details_in_production(int statusCode)
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();

        bool handled = await handler.TryHandleAsync(
            context,
            new BadHttpRequestException("internal-detail-that-must-not-leak", statusCode),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(statusCode);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        string body = await ReadBodyAsync(context);
        body.Should().NotContain("internal-detail-that-must-not-leak");
        body.Should().Contain($"\"status\":{statusCode}");
    }

    [Fact]
    public async Task Malformed_form_data_failures_map_to_400_without_details_in_production()
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidDataException("internal-multipart-detail-that-must-not-leak"),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        string body = await ReadBodyAsync(context);
        body.Should().NotContain("internal-multipart-detail-that-must-not-leak");
    }

    [Fact]
    public async Task Json_deserialization_failures_map_to_400_without_details_in_production()
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();

        bool handled = await handler.TryHandleAsync(
            context,
            new JsonException("internal-json-detail-that-must-not-leak"),
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        string body = await ReadBodyAsync(context);
        body.Should().NotContain("internal-json-detail-that-must-not-leak");
    }

    [Fact]
    public async Task Development_reports_the_exception_message_while_other_environments_do_not()
    {
        (HostRequestExceptionHandler developmentHandler, DefaultHttpContext developmentContext) = Create(Environments.Development);
        (HostRequestExceptionHandler productionHandler, DefaultHttpContext productionContext) = Create(Environments.Production);

        _ = await developmentHandler.TryHandleAsync(developmentContext, new BadHttpRequestException("development-detail", StatusCodes.Status400BadRequest), CancellationToken.None);
        _ = await productionHandler.TryHandleAsync(productionContext, new BadHttpRequestException("development-detail", StatusCodes.Status400BadRequest), CancellationToken.None);

        (await ReadBodyAsync(developmentContext)).Should().Contain("development-detail");
        (await ReadBodyAsync(productionContext)).Should().NotContain("development-detail");
    }

    [Fact]
    public async Task Client_cancellation_maps_to_499()
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();
        context.RequestAborted = new CancellationToken(canceled: true);

        bool handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status499ClientClosedRequest);
    }

    [Fact]
    public async Task Cancellation_that_is_not_a_client_abort_stays_unhandled()
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();

        bool handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Genuine_server_bugs_stay_unhandled_so_they_are_not_masked()
    {
        (HostRequestExceptionHandler handler, DefaultHttpContext context) = Create();

        bool handled = await handler.TryHandleAsync(context, new InvalidOperationException("server-bug"), CancellationToken.None);

        handled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    private static (HostRequestExceptionHandler Handler, DefaultHttpContext Context) Create(string? environmentName = null)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddProblemDetails();
        ServiceProvider provider = services.BuildServiceProvider();
        Mock<IHostEnvironment> environment = new();
        _ = environment.SetupGet(value => value.EnvironmentName).Returns(environmentName ?? Environments.Production);
        HostRequestExceptionHandler handler = new(
            provider.GetRequiredService<IProblemDetailsService>(),
            environment.Object,
            provider.GetRequiredService<ILogger<HostRequestExceptionHandler>>());
        DefaultHttpContext context = new() { RequestServices = provider };
        context.Response.Body = new MemoryStream();
        return (handler, context);
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
