using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Abstractions;

public interface IFileInputOptionsFactory
{
    FileInputOptions Create(FileSelectionMode mode, bool hidden);
}
