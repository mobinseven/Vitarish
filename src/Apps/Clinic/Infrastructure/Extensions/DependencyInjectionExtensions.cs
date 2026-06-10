using Microsoft.Extensions.DependencyInjection;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Clinic.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddClinicInfrastructure(this IServiceCollection services)
        {
            _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SeaDbContext).Assembly));

            return services;
        }
    }
}
