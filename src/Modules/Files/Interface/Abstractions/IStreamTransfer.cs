using Microsoft.AspNetCore.Components;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IStreamTransfer
{
    bool Transferring { get; }
    RenderFragment ActContent { get; }

    IReadOnlyList<PendingFileDto> PendingFiles { get; }
    IReadOnlyList<FileInformationReadDto> UploadedFiles { get; }
    Func<Task>? OnStateChangedAsync { get; set; }
    Func<FileInformationReadDto, Task>? OnFileUploadedAsync { get; set; }
    string? UserId { get; set; }

    void ClearPendings();
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default);
    void SetActivatorContent(RenderFragment? activatorContent);
    Task StartTransferAsync(CancellationToken cancellationToken = default);
    string GetInputId();
}
