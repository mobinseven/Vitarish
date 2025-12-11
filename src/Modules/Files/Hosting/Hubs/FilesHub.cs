namespace Vitarish.Files.Hosting.Hubs;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Application.Abstractions;

public sealed class FilesHub(IFilesService filesService, IFileInformationService fileInformationService, IEntityUserService entityUserService)
    : EntityServer<FileInformationDto, FileInformationDescriptorDto, IFileInformationService>(fileInformationService, entityUserService)
{
    public Task<FileInformationDto?> StreamFileAsync(
        ChannelReader<byte[]> data,
        FileInformationDto fileInformation) => filesService.SaveFile(
            data,
            fileInformation,
            CancellationToken.None);
}
