using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Reminders.Application.Abstractions;
using Vitarish.Reminders.Infrastructure.Data;
using Vitarish.Reminders.Infrastructure.DependencyInjection;
using Vitarish.Reminders.Infrastructure.Serializers;
using Vitarish.Reminders.Interface.DependencyInjection;

namespace Vitarish.Reminders.Interface;

public class RemindersModule : Module, IRemindersModule
{
    public override string Name => "Reminders";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishRemindersDomainEntityHandlers();

        _ = builder.Services.AddSingleton<IRemindersModule, RemindersModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddDbContext<RemindersDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);

        _ = builder.Services.AddRemindersInfrastructure();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, RemindersJsonSerializerContext.Default));

        _ = builder.Services.ConfigureRemindersInterface();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
