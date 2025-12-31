using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Services;

namespace Vitarish.Files.Interface.DependencyInjection;

public static class FilesModuleExtensions
{
    public static IServiceCollection AddFilesModuleComponents(this IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddScoped<StreamTransferService>();
        _ = services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        return services;
    }
}
