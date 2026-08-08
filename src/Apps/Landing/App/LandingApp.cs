using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Apps.Base.Core;
using Vitarish.Landing.Api;
using Vitarish.Landing.Application;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;

namespace Vitarish.Landing.App;

public sealed class LandingApp : Vitarish.Apps.Base.Core.App
{
    public override string Name => "Landing";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddOptions<LandingPlatformOptions>()
            .Validate(options => Uri.TryCreate(options.LandingUrl, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.IsLoopback),
                "A valid HTTPS LandingUrl is required.")
            .ValidateOnStart();
        _ = builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(RunLandingConversationHandler).Assembly));
        _ = builder.Services.AddOptions<LandingConversationAgentOptions>()
            .Configure<IOptions<AiOptions>>((landing, intelligence) => landing.Model = intelligence.Value.DefaultResponseModel)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "The landing conversation model is required.")
            .ValidateOnStart();
        _ = builder.Services.AddRateLimiter(options => options.AddPolicy(LandingRateLimitPolicies.PublicConversation, context => RateLimitPartition.GetSlidingWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new SlidingWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, SegmentsPerWindow = 6, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst })));
        return Task.CompletedTask;
    }
    public override async Task Initialize(WebApplication app)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"), "true", StringComparison.OrdinalIgnoreCase))
        {
            _ = app.MapLandingApi();
            return;
        }

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        LandingPlatformOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<LandingPlatformOptions>>().Value;
        IAppTenantProvisioner provisioner = scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>();
        IPartiesService parties = scope.ServiceProvider.GetRequiredService<IPartiesService>();
        Uri landingUrl = new(options.LandingUrl, UriKind.Absolute);
        LandingHostBinding binding = await parties.GetLandingHostBindingAsync(landingUrl.Host, CancellationToken.None);
        if (binding.TenantId is null)
        {
            _ = await provisioner.EnsureAsync(new EnsureAppTenantRequest(
                LandingTenant.OwnerName,
                LandingTenant.AppId,
                LandingTenant.Name,
                landingUrl.Host), CancellationToken.None);
        }
        _ = app.MapLandingApi();
    }
}
