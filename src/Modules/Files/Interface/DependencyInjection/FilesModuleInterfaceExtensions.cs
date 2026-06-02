using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Interface.Services;

namespace Vitarish.Files.Interface.DependencyInjection;

public static class FilesModuleExtensions
{
    public static IServiceCollection ConfigureFilesInterface(this IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddScoped<StreamTransferService>();
        _ = services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        return services;
    }
}
