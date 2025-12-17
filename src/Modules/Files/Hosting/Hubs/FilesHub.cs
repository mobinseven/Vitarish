namespace Vitarish.Files.Hosting.Hubs;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Application.Abstractions;

public sealed class FilesHub(IServiceScopeFactory serviceScopeFactory, IFilesService filesService, IFileInformationService fileInformationService, IEntityUserService entityUserService)
    : EntityServer<FileInformationDto, FileInformationWriteDto, FileInformationDescriptorDto, IFileInformationService>(serviceScopeFactory, entityUserService)
{
    public Task<FileInformationDto?> StreamFileAsync(
        ChannelReader<byte[]> data,
        FileInformationDto fileInformation) => filesService.SaveFile(
            data,
            fileInformation,
            CancellationToken.None);
}
