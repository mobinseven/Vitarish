using Vitarish.Files.Domain.Models;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IFileSelectionSession
{
    IReadOnlyList<PendingFileDto> PendingFiles { get; }

    void Clear();

    void ReplaceWith(List<PendingFileDto> pendingFiles, FileSelectionMode mode);

    bool Remove(PendingFileDto pendingFile, FileSelectionMode mode);
}
