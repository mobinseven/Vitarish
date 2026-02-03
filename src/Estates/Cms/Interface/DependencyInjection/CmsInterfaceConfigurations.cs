namespace Vitarish.Cms.Interface.DependencyInjection;

public static class CmsInterfaceConfigurations
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureCmsInterface()
        {
            return services;
        }
    }
}
