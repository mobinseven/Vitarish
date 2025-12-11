using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Interface.Files.Abstractions;
using Vitarish.Clients.Interface.Files.Services;
using Vitarish.Clients.Modules.Files.DependencyInjection;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Interface.Files.DependencyInjection;

public static class FilesClientExtensions
{
    public static WebApplicationBuilder AddFilesClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddFilesModuleClient();
        _ = builder.Services.AddScoped<StreamTransferService>();
        _ = builder.Services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddScoped<IEntityInterceptionProvider, AmbientEntityInterceptionProvider>();
        return builder;
    }
}
