using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Options;
using Vitarish.Communications.Api;
using System.Threading.RateLimiting;
using Vitarish.Communications.Infrastructure.Data;
using Vitarish.Communications.Infrastructure.DependencyInjection;
using Vitarish.Communications.Infrastructure.Serializers;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Communications.Module;

public class CommunicationModule : Modules.Base.Core.Module, ICommunicationsModule
{
    public override string Name => "Communications";

    public override void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath)
    {
        _ = services.AddOptions<ProvidersOptions>()
            .BindConfiguration($"{sectionPath}:Providers")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        _ = services.AddOptions<NotificationOptions>()
            .BindConfiguration($"{sectionPath}:Notifications")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<CommunicationsDbContext>();
        _ = app.MapCommunicationsApi();
    }
    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        RegisterOptions(builder.Services, builder.Configuration, OptionsSectionPath);
        _ = builder.Services.AddRateLimiter(options =>
            options.AddPolicy(
                ContactInquiryRateLimitPolicies.PublicInquiry,
                context => RateLimitPartition.GetSlidingWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(10),
                        PermitLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 10
                    })));
        _ = builder.Services.AddSingleton<ICommunicationsModule, CommunicationModule>();
        _ = builder.Services.AddTransient<IMessagesService, MessagesService>();
        _ = builder.Services.AddTransient<ISmsProvider, SmsDotIrProvider>();

        builder.Services.AddVitarishCommunicationsDomainEntityHandlers();

        _ = builder.Services.AddTransientMultiTenantDbContext<CommunicationsDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddRemindersInfrastructure();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, CommunicationsJsonSerializerContext.Default));

    }
}

