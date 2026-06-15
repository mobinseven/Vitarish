using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Infrastructure.Data;
using Vitarish.Communications.Infrastructure.DependencyInjection;
using Vitarish.Communications.Infrastructure.Serializers;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;
using Vitarish.Communications.Interface.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Communications.Module;

public class CommunicationModule : Vitarish.Modules.Base.Module, ICommunicationsModule
{
    public override string Name => "Communications";

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ICommunicationsModule, CommunicationModule>();
        _ = builder.Services.AddTransient<IMessagesService, MessagesService>();
        _ = builder.Services.AddTransient<ISmsProvider, SmsDotIrProvider>();

        builder.Services.AddVitarishCommunicationsDomainEntityHandlers();

        _ = builder.Services.AddTransientMultiTenantDbContext<CommunicationsDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddRemindersInfrastructure();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, CommunicationsJsonSerializerContext.Default));

        _ = builder.Services.ConfigureCommunicationsInterface();
    }
}

