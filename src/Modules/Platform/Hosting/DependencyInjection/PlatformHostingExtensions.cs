using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Platform.Hosting.DependencyInjection;

public static class PlatformHostingExtensions
{
    public static void AddPlatformCircuitMemoryObserver(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<CircuitHandler, PlatformCircuitMemoryObserver>();
    }
}
