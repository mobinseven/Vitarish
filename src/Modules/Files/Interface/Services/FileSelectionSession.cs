using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Services;

public sealed class FileSelectionSession : IFileSelectionSession
{
    private List<PendingFileDto> _pendingFiles = [];

    public IReadOnlyList<PendingFileDto> PendingFiles => _pendingFiles;

    public void Clear() => _pendingFiles.Clear();

    public bool Remove(PendingFileDto pendingFile, FileSelectionMode mode)
    {
        if (mode == FileSelectionMode.SingleCapture)
        {
            _pendingFiles.RemoveAll(x => x.Name == pendingFile.Name);
            return true;
        }

        return _pendingFiles.Remove(pendingFile);
    }

    public void ReplaceWith(List<PendingFileDto> pendingFiles, FileSelectionMode mode)
    {
        _pendingFiles = mode == FileSelectionMode.SingleCapture
            ? pendingFiles.Take(1).ToList()
            : pendingFiles;
    }
}
