namespace Vitarish.Social.Infrastructure.DependencyInjection;

using Vitarish.Social.Infrastructure.Adapters;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSocialInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishSocialInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SocialDbContext).Assembly));
        _ = services.AddHttpClient(nameof(TelegramSocialProviderAdapter));
        _ = services.AddHttpClient(nameof(InstagramSocialProviderAdapter));
        return services;
    }
}
