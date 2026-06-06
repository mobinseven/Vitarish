using Microsoft.JSInterop;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IFileInputInterop : IAsyncDisposable
{
    Task EnsureInitializedAsync(string inputId, object selfRef, FileInputOptions options, CancellationToken cancellationToken = default);

    Task RemoveFileAsync(string inputId, string fileName, CancellationToken cancellationToken = default);

    Task<IJSStreamReference?> GetFileStreamAsync(string inputId, string fileName, CancellationToken cancellationToken = default);

    Task DisposeInputAsync(string inputId);
}
