using Vitarish.Files.Contracts.Metadata;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IFileTransferCoordinator
{
    Task StartTransferAsync(
        IReadOnlyList<PendingFileDto> pendingFiles,
        string inputId,
        FileSelectionMode mode,
        Func<Task> onProgressAsync,
        Func<FileInformationDto, Task> onUploadedAsync,
        Func<PendingFileDto, FileSelectionMode, bool> onPendingRemoved,
        CancellationToken cancellationToken = default);
}
