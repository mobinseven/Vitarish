namespace Vorn.Files.Host.Presentation.Hub;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Host.Infrastructure.Interfaces;

public sealed class FilesHub(IFilesService filesService, IFileInformationService fileInformationService, IEntityUserService entityUserService)
    : EntityServer<FileInformationDto, FileInformationDescriptorDto, IFileInformationService>(fileInformationService, entityUserService)
{
    public Task<FileInformationDto?> StreamFileAsync(
        ChannelReader<byte[]> data,
        FileInformationDto fileInformation) => filesService.SaveFile(
            data,
            fileInformation,
            CancellationToken.None) ?? throw new InvalidOperationException("Failed to save file");
}
