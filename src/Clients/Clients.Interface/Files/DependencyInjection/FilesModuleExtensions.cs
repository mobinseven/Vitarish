using Vitarish.Clients.Interface.Files.Abstractions;
using Vitarish.Clients.Interface.Files.Services;

namespace Vitarish.Clients.Interface.Files.DependencyInjection;

public static class FilesModuleExtensions
{
    public static IServiceCollection AddFilesModuleComponents(this IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddScoped<StreamTransferService>();
        _ = services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        _ = services.AddScoped<IEntityInterceptionProvider, AmbientEntityInterceptionProvider>();
        return services;
    }
}
