using System.Threading.Channels;
using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Infrastructure.Interfaces
{
    public interface IFilesService
    {
        Task<FileInformationDto?> SaveFile(ChannelReader<byte[]> file, FileInformationDto fileInformation, CancellationToken cancellationToken = default);
    }
}
