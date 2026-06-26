using Vitarish.Files.Contracts.Metadata;
using Vitarish.Files.Contracts.Requests;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Contracts.Results;

namespace Vitarish.Files.Client;

public interface IFilesClient
{
    Task<FilesResult<FileInformationDto>> GetFileInformationAsync(Guid fileId, CancellationToken cancellationToken);
    Task<FilesResult<IReadOnlyList<FileInformationDto>>> FindFilesAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken);
    Task<FilesResult<FileInformationDto>> CreateFileRecordAsync(FileInformationWriteRequest request, CancellationToken cancellationToken);
    Task<FilesResult<FileUploadResponse>> UploadFileAsync(FileUploadRequest request, Stream content, CancellationToken cancellationToken);
    Task<FilesResult<Stream>> DownloadFileAsync(Guid fileId, CancellationToken cancellationToken);
}
