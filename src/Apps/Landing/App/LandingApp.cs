using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Apps.Base.Core;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Landing.Api;
using Vitarish.Landing.Application;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

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
        _ = builder.Services.AddTransient<Vitarish.Authentication.Application.Abstractions.IRoleFreeSessionPolicy, LandingHostRoleFreeSessionPolicy>();
        _ = builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(RunLandingConversationHandler).Assembly));
        _ = builder.Services.AddOptions<LandingConversationAgentOptions>()
            .Configure<IOptions<AiOptions>>((landing, intelligence) => landing.Model = intelligence.Value.DefaultResponseModel)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "The landing conversation model is required.")
            .ValidateOnStart();
        _ = builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy(LandingRateLimitPolicies.PublicConversation, context => RateLimitPartition.GetSlidingWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new SlidingWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, SegmentsPerWindow = 6, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst }));
            options.AddPolicy(LandingRateLimitPolicies.PublicCatalog, context => RateLimitPartition.GetSlidingWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new SlidingWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 60, SegmentsPerWindow = 6, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst }));
            options.AddPolicy(LandingRateLimitPolicies.AuthenticatedVisitor, context => RateLimitPartition.GetSlidingWindowLimiter(
                LandingRateLimitPolicies.AuthenticatedVisitorPartitionKey(context),
                _ => new SlidingWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, SegmentsPerWindow = 6, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst }));
        });
        return Task.CompletedTask;
    }
    public override async Task Initialize(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        LandingPlatformOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<LandingPlatformOptions>>().Value;
        IAppTenantProvisioner provisioner = scope.ServiceProvider.GetRequiredService<IAppTenantProvisioner>();
        IPartiesService parties = scope.ServiceProvider.GetRequiredService<IPartiesService>();
        Uri landingUrl = new(options.LandingUrl, UriKind.Absolute);
        LandingHostBinding binding = await parties.GetLandingHostBindingAsync(landingUrl.Host, CancellationToken.None);
        Guid tenantId = binding.TenantId ?? await provisioner.EnsureAsync(new EnsureAppTenantRequest(
            LandingTenant.OwnerName,
            LandingTenant.AppId,
            LandingTenant.Name,
            landingUrl.Host), CancellationToken.None);

        try
        {
            bool tenantScopeFound = false;
            string tenantIdValue = tenantId.ToString();
            TenantBackgroundExecutor tenantBackgroundExecutor = scope.ServiceProvider.GetRequiredService<TenantBackgroundExecutor>();
            await tenantBackgroundExecutor.ExecutePerTenantAsync(
                LandingContentSeed.BackgroundResource,
                async (tenantServices, cancellationToken) =>
                {
                    IRequestContext requestContext = tenantServices.GetRequiredService<IRequestContext>();
                    if (requestContext.TenantId is not string requestTenantId ||
                        !string.Equals(requestTenantId, tenantIdValue, StringComparison.Ordinal))
                    {
                        return;
                    }

                    tenantScopeFound = true;
                    await LandingContentSeed.EnsureAsync(
                        tenantServices.GetRequiredService<IContentSeeding>(),
                        requestTenantId,
                        cancellationToken);
                },
                CancellationToken.None);
            if (!tenantScopeFound)
            {
                throw new InvalidOperationException("The provisioned Landing tenant was not available to the background tenant executor.");
            }
        }
        catch (Exception exception)
        {
            ILogger<LandingApp> logger = scope.ServiceProvider.GetRequiredService<ILogger<LandingApp>>();
            logger.LogError(exception, "Failed to seed landing content for tenant {TenantId}; the host will continue starting.", tenantId);
        }
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapLandingApi();
}
