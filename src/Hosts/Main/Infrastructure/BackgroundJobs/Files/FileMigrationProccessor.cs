using Vitarish.Entities.Server.Services;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Sea.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Files;

public class FileMigrationProccessor(
    TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:sea:files:migration";
    public async Task ProcessMigration(CancellationToken cancellationToken = default)
    {
        static async Task perTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
        {
            await ProcessTenantMigrationAsync(
                provider.GetRequiredService<IFileInformationService>(),
                provider.GetRequiredService<IObjectStorageService>(),
                provider.GetRequiredService<IOptions<FilesOptions>>(),
                shutdownToken);
        }

        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            perTenantAsync,
            cancellationToken);
    }
    private static async Task ProcessTenantMigrationAsync(
    IFileInformationService fileInformationService,
    IObjectStorageService objectStorageService,
    IOptions<FilesOptions> options,
    CancellationToken cancellationToken)
    {
        var fileInfos = await fileInformationService.GetListAsync(new Vitarish.Files.Domain.Models.FileInformationDescriptorDto(), cancellationToken).GetValueOrDefaultAsync() ?? [];
        foreach (var fileInfo in fileInfos)
        {

            string id = FilesExtensions.ExtractId(fileInfo.Url, "VITARISH");
            string fileName = Path.GetFileName(fileInfo.Url);
            fileInfo.Tag = id;
            FileInformationWriteDto fileInformationWriteDto = new()
            {
                Id = fileInfo.Id,
                AllowedOrigins = fileInfo.AllowedOrigins,
                Checksum = fileInfo.Checksum,
                Owner = fileInfo.Owner,
                Url = fileName,
                Thumbnail = fileInfo.Thumbnail,
                ContentType = fileInfo.ContentType,
                Tag = fileInfo.Tag
            };

            await fileInformationService.UpdateAsync(fileInformationWriteDto, cancellationToken);
            await objectStorageService.RenameAsync(options.Value.BuildObjectKey(fileName), fileInfo.Checksum, cancellationToken);
        }
    }
}
