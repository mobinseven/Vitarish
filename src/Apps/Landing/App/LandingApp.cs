using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Apps.Base.Core;
using Vitarish.Landing.Api;
using Vitarish.Landing.Application;

namespace Vitarish.Landing.App;
public sealed class LandingApp : Vitarish.Apps.Base.Core.App
{
    public override string Name => "Landing";
    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(RunLandingConversationHandler).Assembly));
        _ = builder.Services.AddRateLimiter(options => options.AddPolicy(LandingRateLimitPolicies.PublicConversation, context => RateLimitPartition.GetSlidingWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new SlidingWindowRateLimiterOptions { Window = TimeSpan.FromMinutes(1), PermitLimit = 10, SegmentsPerWindow = 6, QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst })));
        return Task.CompletedTask;
    }
    public override Task Initialize(WebApplication app) { _ = app.MapLandingApi(); return Task.CompletedTask; }
}
