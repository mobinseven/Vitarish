using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.DependencyInjection;
using Vitarish.Appointments.Infrastructure.Serializers;
using Vitarish.Appointments.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Appointments.Interface;

public class AppointmentsModule :Module, IAppointmentsModule
{
    public override string Name => "Appointments";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishAppointmentsDomainEntityHandlers();

        _ = builder.Services.AddSingleton<IAppointmentsModule, AppointmentsModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddDbContext<AppointmentsDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);

        _ = builder.Services.AddAppointmentsInfrastructure();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppointmentsJsonSerializerContext.Default));

        _ = builder.Services.ConfigureAppointmentsInterface();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
