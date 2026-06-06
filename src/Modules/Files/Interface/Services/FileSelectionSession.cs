using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Services;

public sealed class FileSelectionSession : IFileSelectionSession
{
    private readonly object _lock = new();
    private List<PendingFileDto> _pendingFiles = [];

    public IReadOnlyList<PendingFileDto> PendingFiles
    {
        get
        {
            lock (_lock)
            {
                return _pendingFiles.ToList();
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _pendingFiles.Clear();
        }
    }

    public bool Remove(PendingFileDto pendingFile, FileSelectionMode mode)
    {
        lock (_lock)
        {
            if (mode == FileSelectionMode.SingleCapture)
            {
                return _pendingFiles.RemoveAll(x => x.Name == pendingFile.Name) > 0;
            }

            return _pendingFiles.Remove(pendingFile);
        }
    }

    public void ReplaceWith(List<PendingFileDto> pendingFiles, FileSelectionMode mode)
    {
        lock (_lock)
        {
            _pendingFiles = mode == FileSelectionMode.SingleCapture
                ? pendingFiles.Take(1).ToList()
                : pendingFiles.ToList();
        }
    }
}
