using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Models;

namespace Vitarish.Files.Interface.Services;

public sealed class FileInputOptionsFactory : IFileInputOptionsFactory
{
    private const string MediaAccept = "image/*,video/*";

    public FileInputOptions Create(FileSelectionMode mode, bool hidden)
        => mode switch
        {
            FileSelectionMode.SingleCapture => new FileInputOptions { Accept = MediaAccept, Multiple = false, Capture = "environment", Hidden = hidden },
            FileSelectionMode.MultiGallery => new FileInputOptions { Accept = MediaAccept, Multiple = true, Capture = null, Hidden = hidden },
            _ => new FileInputOptions { Accept = MediaAccept, Multiple = true, Capture = null, Hidden = hidden }
        };
}
