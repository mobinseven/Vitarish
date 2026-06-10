using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Clinic.Infrastructure.Handlers;

namespace Vitarish.Clinic.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddClinicInfrastructure(this IServiceCollection services)
        {
            _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ClinicDbContext).Assembly));
            _ = services.AddTransient<PatientViewQueryShape>();

            return services;
        }
    }
}
