using Microsoft.AspNetCore.Components;
using Vorn.Files.Client.Models;

namespace Vorn.Files.Client.Abstractions;

public interface IStreamTransfer
{
    bool Transferring { get; }
    RenderFragment ActContent { get; }

    IReadOnlyList<PendingFileDto> PendingFiles { get; }
    IReadOnlyList<FileInformationDto> UploadedFiles { get; }
    Func<Task>? OnStateChangedAsync { get; set; }
    Func<FileInformationDto, Task>? OnFileUploadedAsync { get; set; }
    string? UserId { get; set; }

    void ClearPendings();
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default);
    void SetActivatorContent(RenderFragment? activatorContent);
    Task StartTransferAsync(CancellationToken cancellationToken = default);
}
