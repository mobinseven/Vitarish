using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Vitarish.Host.Services;
using Vitarish.Users.Infrastructure.Service;

public static class HostPipelineExtensions
{
    public static void AddHostPipeline(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        _ = builder.Services.AddSingleton<EventPublisher>();

        _ = builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = 1;
            // TODO: Configure KnownProxies/KnownNetworks for production deployment
            // options.KnownProxies.Add(IPAddress.Parse("proxy-ip"));
        });

        // Rate limiting for endpoints
        _ = builder.Services.AddRateLimiter(options =>
        {
            // On-demand TLS /allow endpoint
            _ = options.AddSlidingWindowLimiter("allow-endpoint", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 50;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
                opt.SegmentsPerWindow = 6;
            });

            // Tenant resolve endpoint - higher limit for authenticated clients
            _ = options.AddSlidingWindowLimiter("tenant-resolve", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 100;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
                opt.SegmentsPerWindow = 6;
            });
        });
    }
}
