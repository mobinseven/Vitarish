using System.Threading.Channels;
using Vitarish.Clients.Modules.Files.Models;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Files.Abstractions;

/// <summary>
/// Client abstraction for file related operations exposed by the files service.
/// </summary>
public interface IFilesModuleClient : IEntityClient<FileInformationDto, FileInformationDescriptorDto>
{
    /// <summary>
    /// Streams a file to the server using a channel of byte arrays and returns the resulting file information.
    /// </summary>
    /// <param name="dataReader">A channel reader that yields chunks of the file as byte arrays.</param>
    /// <param name="pendingFile">Metadata describing the file being uploaded.</param>
    /// <param name="ct">Cancellation token to cancel the upload operation.</param>
    /// <returns>The stored <see cref="FileInformationDto"/> representing the uploaded file.</returns>
    Task<FileInformationDto> StreamFileAsync(ChannelReader<byte[]> dataReader, PendingFileDto pendingFile, CancellationToken ct = default);
}
