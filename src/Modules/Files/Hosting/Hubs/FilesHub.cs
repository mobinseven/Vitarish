namespace Vitarish.Files.Hosting.Hubs;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Application.Abstractions;

public sealed class FilesHub(IFileInformationService fileInformationService, IFilesService filesService, IEntityUserService entityUserService)
    : EntityServer<FileInformationDto, FileInformationWriteDto, FileInformationDescriptorDto, IFileInformationService>(fileInformationService, entityUserService)
{
    public Task<FileInformationDto?> StreamFileAsync(
        ChannelReader<byte[]> data,
        FileInformationDto fileInformation) => filesService.SaveFile(
            data,
            fileInformation,
            CancellationToken.None);
}
