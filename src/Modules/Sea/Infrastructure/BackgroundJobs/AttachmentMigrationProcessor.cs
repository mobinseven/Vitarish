using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

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
                provider.GetRequiredService<IAttachmentsService>(),
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
        IAttachmentsService attachmentsService,
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

                _ = ExtractFileId(fileInfo.Url, "VITARISH");
                if (string.IsNullOrWhiteSpace(fileInfo.Checksum))
                {
                    continue;
                }

                string fileName = Path.GetFileName(fileInfo.Url);
                IEnumerable<AttachmentReadDto> attchs = attachments.Where(a => a.Content.Contains(fileName));
                foreach (AttachmentReadDto? attch in attchs)
                {
                    AttachmentWriteDto wrt = attch.ToWrite() with { Content = fileInfo.Id.ToString() };
                    _ = await attachmentsService.UpdateAsync(wrt, cancellationToken);
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

    private static string ExtractFileId(string path, string prefix)
    {
        string fileName = Path.GetFileName(path);
        string expectedPrefix = prefix + "-";
        if (!fileName.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("The given path does not have the expected prefix.");
        }

        string withoutPrefix = fileName[expectedPrefix.Length..];
        int dotIndex = withoutPrefix.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex < 0)
        {
            throw new FormatException("The given file name does not have an extension.");
        }

        return withoutPrefix[..dotIndex];
    }
}
