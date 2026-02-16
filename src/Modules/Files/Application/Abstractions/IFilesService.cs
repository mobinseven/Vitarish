using System.Threading.Channels;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Infrastructure.Interfaces
{
    public interface IFilesService
    {
        Task<FileInformationReadDto?> SaveFile(ChannelReader<byte[]> file, FileInformationReadDto fileInformation, CancellationToken cancellationToken = default);
    }
}
