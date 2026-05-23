using Vitarish.Appointments.Application.Abstractions.BookingPolicies;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Appointments.Infrastructure.Repositories;
using Vitarish.Appointments.Infrastructure.Handlers;

namespace Vitarish.Appointments.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAppointmentsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishAppointmentsInfrastructure();
        _ = services.AddTransient<IAccessControlledShiftManager, AccessControlledShiftManager>();
        _ = services.AddTransient<IAccessControlledReservationManager, AccessControlledReservationManager>();
        _ = services.AddTransient<IShiftBookingPolicyEngine, ShiftBookingPolicyEngine>();
        _ = services.AddTransient<IEntityRepository<ShiftBookingPolicy, ShiftBookingPolicyDescriptor>, ShiftBookingPolicyRepository>();
        _ = services.AddTransient<IEntityRepository<ShiftReminderPolicy, ShiftReminderPolicyDescriptor>, ShiftReminderPolicyRepository>();
        _ = services.AddTransient<IServiceTypeManager, ServiceTypeManager>();
        _ = services.AddTransient<IServiceManager, ServiceManager>();
        _ = services.AddTransient<IServiceProviderManager, ServiceProviderManager>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AppointmentsDbContext).Assembly));
        return services;
    }
}
