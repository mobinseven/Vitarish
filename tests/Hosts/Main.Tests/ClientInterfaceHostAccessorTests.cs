using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Application.Abstractions;

public sealed class ClientInterfaceHostAccessorTests
{
    [Fact]
    public void Returns_only_the_hostname_for_an_allowed_origin_with_a_non_default_port()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddHttpContextAccessor();
        builder.AddHostCors(["http://localhost:3006"]);
        using WebApplication app = builder.Build();
        IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Request.Headers.Origin = "http://localhost:3006";

        IClientInterfaceHostAccessor clientInterfaceHost = app.Services.GetRequiredService<IClientInterfaceHostAccessor>();

        Assert.Equal("localhost", clientInterfaceHost.ClientInterfaceHost);
    }

    [Fact]
    public void Rejects_client_origin_outside_the_exact_allowlist()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddHttpContextAccessor();
        builder.AddHostCors(["http://localhost:3006"]);
        using WebApplication app = builder.Build();
        IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Request.Headers.Origin = "http://localhost:3005";

        IClientInterfaceHostAccessor clientInterfaceHost = app.Services.GetRequiredService<IClientInterfaceHostAccessor>();

        Assert.Null(clientInterfaceHost.ClientInterfaceHost);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-origin")]
    [InlineData("http://localhost:3006/")]
    [InlineData("http://localhost:3006/path")]
    [InlineData("http://localhost:3006/./")]
    [InlineData("http://user@localhost:3006")]
    [InlineData("http://localhost:3006, http://localhost:3006")]
    public void Rejects_absent_malformed_or_non_single_origins(string? origin)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddHttpContextAccessor();
        builder.AddHostCors(["http://localhost:3006"]);
        using WebApplication app = builder.Build();
        IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext = new DefaultHttpContext();
        if (origin is not null)
            httpContextAccessor.HttpContext.Request.Headers.Origin = origin;

        IClientInterfaceHostAccessor clientInterfaceHost = app.Services.GetRequiredService<IClientInterfaceHostAccessor>();

        Assert.Null(clientInterfaceHost.ClientInterfaceHost);
    }
}
