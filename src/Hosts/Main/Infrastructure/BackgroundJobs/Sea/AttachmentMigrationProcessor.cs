using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Processes migration of Attachments from URL-based content references to ID-based references.
/// </summary>
public class AttachmentMigrationProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:sea:attachments:migration";

    /// <summary>
    /// Scans all Attachments and migrates those with URL-based content to ID-based content.
    /// </summary>
    public async Task ProcessMigration(CancellationToken cancellationToken = default)
    {
        static async Task perTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
        {
            await ProcessTenantMigrationAsync(
                provider.GetRequiredService<IOptions<FilesOptions>>(),
                provider.GetRequiredService<IEventsService>(),
                provider.GetRequiredService<IAttachmentsService>(),
                provider.GetRequiredService<IRequestContext>(),
                provider.GetRequiredService<IFileInformationService>(),
                provider.GetRequiredService<IObjectStorageService>(),
                shutdownToken);
        }

        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            perTenantAsync,
            cancellationToken);
    }

    private static async Task ProcessTenantMigrationAsync(
        IOptions<FilesOptions> options,
        IEventsService eventsService,
        IAttachmentsService attachmentsService,
        IRequestContext requestContext,
        IFileInformationService fileInformationService,
        IObjectStorageService objectStorageService,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FileInformationReadDto> fileInfos = await fileInformationService.GetListAsync(new FileInformationDescriptorDto(), cancellationToken).GetValueOrDefaultAsync() ?? [];
        List<FileInformationReadDto> existing = [];
        List<string> errors = [];

        IReadOnlyList<AttachmentReadDto> attachments = await attachmentsService.GetListAsync(new AttachmentDescriptorDto
        {
            Search = "http"
        }, cancellationToken).GetValueOrDefaultAsync() ?? [];


        foreach (FileInformationReadDto? fileInfo in fileInfos)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileInfo.Url))
                {
                    continue;
                }

                string id = FilesExtensions.ExtractId(fileInfo.Url, "VITARISH");
                string fileName = Path.GetFileName(fileInfo.Url);
                IEnumerable<AttachmentReadDto> attchs = attachments.Where(a => a.Content.Contains(fileName));
                foreach (AttachmentReadDto? attch in attchs)
                {
                    AttachmentWriteDto wrt = attch.ToWrite() with { Content = fileInfo.Id.ToString() };
                    await attachmentsService.UpdateAsync(wrt, cancellationToken);
                }

                bool ex = await objectStorageService.ExistsAsync(fileInfo.Checksum, cancellationToken);
                if (ex)
                {
                    existing.Add(fileInfo);
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
