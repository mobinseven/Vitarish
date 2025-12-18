
using System.Threading.Channels;
using Vitarish.Clients.Modules.Files.Abstractions;
using Vitarish.Clients.Modules.Files.Models;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Files.Internal.Services;

internal class FilesModuleClient : EntityClient<FileInformationDto, FileInformationDto, FileInformationDescriptorDto>, IAsyncDisposable, IFilesModuleClient
{
    private readonly IEntityConnectionFactory<FileInformationDto> _connectionFactory;
    private readonly IEntityInterceptionProvider _entityInterceptionProvider;

    public FilesModuleClient(IEntityConnectionFactory<FileInformationDto> connectionFactory, IEntityInterceptionProvider entityInterceptionProvider)
        : base(connectionFactory, entityInterceptionProvider)
    {
        _connectionFactory = connectionFactory;
        _entityInterceptionProvider = entityInterceptionProvider;
    }
    public async Task<FileInformationDto> StreamFileAsync(
        ChannelReader<byte[]> dataReader, PendingFileDto pendingFile, CancellationToken ct = default)
    {
        FileInformationDto info = new()
        {
            Checksum = pendingFile.Checksum,
            Thumbnail = pendingFile.Thumbnail,
            AllowedOrigins = "*",
            ContentType = pendingFile.Type,
            Metadata = new FileMetadata
            {
                Size = pendingFile.Size
            }
        };

        return await InvokeHubAsync<FileInformationDto>(nameof(StreamFileAsync), ct, new object?[] { dataReader, info }).ConfigureAwait(false);
    }
}
