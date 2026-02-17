using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

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
        IReadOnlyList<FileInformationReadDto> fileInfos = await fileInformationService.GetListAsync(new FileInformationDescriptorDto(), cancellationToken).GetValueOrDefaultAsync() ?? [];
        List<FileInformationReadDto> existing = new();
        List<string> errors = new();
        foreach (FileInformationReadDto? fileInfo in fileInfos)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(fileInfo.Url))
                {
                    continue;
                }
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

                _ = await fileInformationService.UpdateAsync(fileInformationWriteDto, cancellationToken);
                bool ex = await objectStorageService.ExistsAsync(fileInfo.Checksum, cancellationToken);
                if (ex)
                {
                    existing.Add(fileInfo);
                    //continue;
                }
                else
                {
                    await objectStorageService.RenameAsync(fileName, fileInfo.Checksum, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{ex.GetType().Name} {ex.Message}");
            }
        }
    }
}
