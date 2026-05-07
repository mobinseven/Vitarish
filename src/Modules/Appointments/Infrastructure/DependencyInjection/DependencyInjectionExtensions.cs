using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;

namespace Vitarish.Appointments.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAppointmentsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishAppointmentsInfrastructure();
        _ = services.AddTransient<IAccessControlledSessionManager, AccessControlledSessionManager>();
        _ = services.AddTransient<IAccessControlledReservationManager, AccessControlledReservationManager>();
        _ = services.AddTransient<IAppointmentReminderService, AppointmentReminderService>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppointmentsDbContext).Assembly));
        return services;
    }
}
