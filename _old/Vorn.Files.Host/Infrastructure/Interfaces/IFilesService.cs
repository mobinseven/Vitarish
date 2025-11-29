using System.Threading.Channels;
using Vorn.Files.Core.Domain.Models;

namespace Vorn.Files.Host.Infrastructure.Interfaces
{
    public interface IFilesService
    {
        Task<FileInformationDto?> SaveFile(ChannelReader<byte[]> file, FileInformationDto fileInformation, CancellationToken cancellationToken = default);
    }
}