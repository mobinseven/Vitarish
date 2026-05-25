namespace Vitarish.Medical.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddMedicalInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishMedicalInfrastructure();


        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(MedicalDbContext).Assembly));

        return services;
    }
}
