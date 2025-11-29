namespace Vorn.Files.Client.Internal.Services;

using System.Threading.Channels;
using Vorn.Entities.Client.SignalR;
using Vorn.Files.Client.Models;

internal class FilesHubClient : EntityClient<FileInformationDto, FileInformationDescriptorDto>, IAsyncDisposable, Vorn.Files.Client.Abstractions.IFilesClient
{
    private readonly IEntityConnectionFactory<FileInformationDto> _connectionFactory;
    private readonly IEntityInterceptionProvider _entityInterceptionProvider;

    public FilesHubClient(IEntityConnectionFactory<FileInformationDto> connectionFactory, IEntityInterceptionProvider entityInterceptionProvider)
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
