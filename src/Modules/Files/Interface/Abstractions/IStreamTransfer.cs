namespace Vitarish.Files.Interface.Abstractions;

public interface IStreamTransfer
{
    bool Transferring { get; }
    string InputId { get; }

    IReadOnlyList<PendingFileDto> PendingFiles { get; }
    IReadOnlyList<FileInformationReadDto> UploadedFiles { get; }
    Func<Task>? OnStateChangedAsync { get; set; }
    Func<FileInformationReadDto, Task>? OnFileUploadedAsync { get; set; }
    FileSelectionMode SelectionMode { get; set; }

    void ClearPendings();
    Task InitializeAsync(bool hiddenInput, CancellationToken cancellationToken = default);
    Task RemovePendingFileAsync(PendingFileDto file, CancellationToken cancellationToken = default);
    Task StartTransferAsync(CancellationToken cancellationToken = default);
}
