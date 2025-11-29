using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Client.Api.DependencyInjection;
using Vorn.Entities.Client.SignalR;
using Vorn.Files.Client.Abstractions;
using Vorn.Files.Client.Internal.Services;
using Vorn.Files.Client.Models;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddFilesClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IFilesClient, FilesHubClient>();
        builder.Services.AddScoped<StreamTransferService>();
        builder.Services.AddScoped<IStreamTransfer>(static sp => sp.GetRequiredService<StreamTransferService>());
        builder.Services.AddScoped<IEntityConnectionFactory<FileInformationDto>, ConnectionFactory>();
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IEntityInterceptionProvider, AmbientEntityInterceptionProvider>();
        builder.AddAaasApiClient();
        builder.Services.AddSignalRClient<FileTransferHub>();
        return builder;
    }
}
