using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Hosting.Infrastructure.Pipeline;

namespace Vitarish.Hosting;

public static class HostExceptionHandlingExtensions
{
    public static WebApplicationBuilder AddHostRequestExceptionHandling(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _ = builder.Services.AddProblemDetails();
        _ = builder.Services.AddExceptionHandler<HostRequestExceptionHandler>();
        return builder;
    }
}
