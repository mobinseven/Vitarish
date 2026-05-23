using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;

namespace Vitarish.Appointments.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAppointmentsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishAppointmentsInfrastructure();
        _ = services.AddTransient<IAccessControlledShiftManager, AccessControlledShiftManager>();
        _ = services.AddTransient<IAccessControlledReservationManager, AccessControlledReservationManager>();
        _ = services.AddTransient<IShiftBookingPolicyManager, ShiftBookingPolicyManager>();
        _ = services.AddTransient<IShiftReminderPolicyManager, ShiftReminderPolicyManager>();
        _ = services.AddTransient<IShiftBookingPolicyEngine, ShiftBookingPolicyEngine>();
        _ = services.AddTransient<IServiceTypeManager, ServiceTypeManager>();
        _ = services.AddTransient<IServiceManager, ServiceManager>();
        _ = services.AddTransient<IServiceProviderManager, ServiceProviderManager>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppointmentsDbContext).Assembly));
        return services;
    }
}
