using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Search.Interface.DependencyInjection;

public static class SearchInterfaceExtensions
{
    /// <summary>
    /// Registers Search interface services (e.g. widget dependencies).
    /// </summary>
    public static IServiceCollection ConfigureSearchInterface(this IServiceCollection services) => services;
}
