using Vitarish.Files.Domain.Models;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IFileTransferCoordinator
{
    Task StartTransferAsync(
        IReadOnlyList<PendingFileDto> pendingFiles,
        string inputId,
        string? userId,
        Func<Task> onProgressAsync,
        Func<FileInformationReadDto, Task> onUploadedAsync,
        Func<PendingFileDto, FileSelectionMode, bool> onPendingRemoved,
        CancellationToken cancellationToken = default);
}
