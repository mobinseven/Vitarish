using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Infrastructure.Services;

namespace Authentication.Tests.Infrastructure;

public sealed class RequestContextClientIpTests
{
    [Fact]
    public void Host_normalizes_ipv4_mapped_client_addresses()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.0.0.7");
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        RequestContext requestContext = new(new HttpContextAccessor { HttpContext = httpContext });

        requestContext.ClientIpAddress.Should().Be("10.0.0.7");
    }

    [Fact]
    public void Host_preserves_ipv6_client_addresses()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::7");
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        RequestContext requestContext = new(new HttpContextAccessor { HttpContext = httpContext });

        requestContext.ClientIpAddress.Should().Be("2001:db8::7");
    }

    [Fact]
    public void Request_context_uses_the_host_resolved_connection_address_instead_of_a_forwarded_header()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.7");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.88";
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        RequestContext requestContext = new(new HttpContextAccessor { HttpContext = httpContext });

        requestContext.ClientIpAddress.Should().Be("198.51.100.7");
    }
}
