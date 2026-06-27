using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Aggregator.Core.DependencyInjection;
using Vitarish.Files.Client;
using Vitarish.Files.Interface.Services;

namespace Vitarish.Files.Interface.DependencyInjection;

public static class FilesModuleExtensions
{
    public static IServiceCollection ConfigureFilesInterface(this IServiceCollection services)
    {
        _ = services.AddMemoryCache();
        _ = services.AddInternalModuleApiClient<IFilesClient, FilesClient>();
        _ = services.AddTransient<IFileSelectionSession, FileSelectionSession>();
        _ = services.AddScoped<IFileInputInterop, FileInputInteropService>();
        _ = services.AddScoped<IFileInputOptionsFactory, FileInputOptionsFactory>();
        _ = services.AddScoped<IFileTransferCoordinator, FileTransferCoordinator>();
        _ = services.AddTransient<StreamTransferService>();
        _ = services.AddTransient<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        return services;
    }
}
