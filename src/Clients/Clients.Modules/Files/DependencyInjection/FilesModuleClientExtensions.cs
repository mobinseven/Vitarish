using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.DependencyInjection;
using Vitarish.Clients.Modules.Files.Abstractions;
using Vitarish.Clients.Modules.Files.Internal.Services;
using Vitarish.Clients.Modules.Files.Models;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Files.DependencyInjection;

public static class FilesModuleClientExtensions
{
    public static IServiceCollection AddFilesModuleClient(this IServiceCollection services)
    {
        _ = services.AddScoped<IFilesModuleClient, FilesModuleClient>();
        _ = services.AddScoped<IEntityConnectionFactory<FileInformationDto>, FilesModuleConnectionFactory>();
        _ = services.AddIdentitySignalRClient<FilesModuleClientDefinition>();
        return services;
    }
}
