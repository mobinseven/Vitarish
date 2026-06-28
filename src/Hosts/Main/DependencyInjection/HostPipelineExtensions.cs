using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Infrastructure.Pipeline;
using Vitarish.Users.Infrastructure.Service;

public static class HostPipelineExtensions
{
    public static void AddHostCors(this WebApplicationBuilder builder)
    {
        CorsOptions corsOptions = builder.Configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>() ?? new();

        string[] origins = corsOptions.AllowedOrigins.ToArray();

        if (Array.Exists(origins, x => x == "*"))
        {
            throw new InvalidOperationException(
                "CORS policy 'WebHostCors' cannot allow the wildcard '*' origin when AllowCredentials() is enabled.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("WebHostCors", policy =>
            {
                policy
                    .WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

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

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.PasswordLogin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpBegin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpComplete, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.Passkey, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy("allow-endpoint", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 50,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy("tenant-resolve", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));
        });
    }
}
