using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Vitarish.Authentication.Api.Endpoints;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Infrastructure.Pipeline;
using Vitarish.Users.Infrastructure.Service;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Hosting;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

public static class HostPipelineExtensions
{
    public static void AddHostCors(this WebApplicationBuilder builder, IReadOnlyCollection<string> exactOrigins)
    {
        string[] origins = exactOrigins.ToArray();

        if (Array.Exists(origins, x => x == "*"))
        {
            throw new InvalidOperationException(
                "CORS policy 'WebHostCors' cannot allow the wildcard '*' origin when AllowCredentials() is enabled.");
        }

        ClientOriginTrust originTrust = new(origins);
        builder.Services.AddSingleton(originTrust);
        builder.Services.AddCors();
        // An exact configured origin, or the tenant address this request's origin trust admitted.
        builder.Services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<IHttpContextAccessor>((options, httpContextAccessor) => options.AddPolicy("WebHostCors", policy =>
            {
                policy
                    .SetIsOriginAllowed(origin => originTrust.IsExact(origin) || ClientOriginTrust.IsTrustedForRequest(httpContextAccessor.HttpContext, origin))
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS")
                    .AllowCredentials();
            }));
        builder.Services.AddSingleton<IClientInterfaceHostAccessor, ClientInterfaceHostAccessor>();
    }

    public static IApplicationBuilder UseClientOriginTrust(this IApplicationBuilder app)
    {
        ClientOriginTrust originTrust = app.ApplicationServices.GetRequiredService<ClientOriginTrust>();
        return app.Use(async (context, next) =>
        {
            await originTrust.EvaluateAsync(context);
            await next(context);
        });
    }

    public static void AddHostPipeline(this WebApplicationBuilder builder, SecurityConfigurationOptions security)
    {
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        _ = builder.Services.AddSingleton<EventPublisher>();

        _ = builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.ForwardLimit = 1;
            foreach (System.Net.IPNetwork network in security.ParseTrustedProxyNetworks())
                options.KnownIPNetworks.Add(network);
        });

        _ = builder.Services.AddRateLimiter(options =>
        {
            options.OnRejected = static (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return ValueTask.CompletedTask;
            };

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.PasswordLogin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpBegin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpComplete, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.Passkey, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy("allow-endpoint", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 50,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy("tenant-resolve", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1), PermitLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst, QueueLimit = 0, SegmentsPerWindow = 6
                    }));
        });
    }

    public static IApplicationBuilder UseInternalServiceEndpointBoundary(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint()?.Metadata.GetMetadata<InternalServiceEndpointMetadata>() is not null &&
                !context.Request.Headers.ContainsKey(InternalServiceEndpointConventions.CredentialHeaderName))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        });

    public static void UseHostWebSockets(this WebApplication app)
    {
        _ = app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
        _ = app.UseTrustedWebSocketOrigins();
    }

    /// <summary>
    /// Replaces <see cref="WebSocketOptions.AllowedOrigins"/>, which only takes a fixed list: a
    /// browser upgrade must come from an origin this request's origin trust admitted. Runs after
    /// <c>UseWebSockets</c> and <see cref="UseClientOriginTrust"/>.
    /// </summary>
    public static IApplicationBuilder UseTrustedWebSocketOrigins(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.WebSockets.IsWebSocketRequest
                && context.Request.Headers.ContainsKey(Microsoft.Net.Http.Headers.HeaderNames.Origin)
                && context.Features.Get<TrustedClientOrigin>() is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context);
        });
}
