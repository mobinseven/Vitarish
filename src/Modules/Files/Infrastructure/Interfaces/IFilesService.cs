using System.Threading.Channels;

namespace Vitarish.Files.Infrastructure.Interfaces
{
    public interface IFilesService
    {
        Task<FileInformationDto?> SaveFile(ChannelReader<byte[]> file, FileInformationDto fileInformation, CancellationToken cancellationToken = default);
    }
}