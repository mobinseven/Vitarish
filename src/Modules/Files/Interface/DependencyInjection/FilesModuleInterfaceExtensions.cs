using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Interface.Services;

namespace Vitarish.Files.Interface.DependencyInjection;

public static class FilesModuleExtensions
{
    public static IServiceCollection ConfigureFilesInterface(this IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddScoped<IFileSelectionSession, FileSelectionSession>();
        _ = services.AddScoped<IFileInputInterop, FileInputInteropService>();
        _ = services.AddScoped<IFileInputOptionsFactory, FileInputOptionsFactory>();
        _ = services.AddScoped<IFileTransferCoordinator, FileTransferCoordinator>();
        _ = services.AddScoped<StreamTransferService>();
        _ = services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        return services;
    }
}
