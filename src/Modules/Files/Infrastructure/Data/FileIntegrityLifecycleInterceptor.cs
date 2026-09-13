using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Files.Domain.Entities;

namespace Vitarish.Files.Infrastructure.Data;

/// <summary>
/// Removes the integrity expectation when an authorized file deletion is saved.
/// </summary>
public sealed class FileIntegrityLifecycleInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        RemoveDeletedFileRecords(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        RemoveDeletedFileRecords(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void RemoveDeletedFileRecords(DbContext? context)
    {
        if (context is not FilesDbContext filesContext)
        {
            return;
        }

        foreach (EntityEntry<FileInformation> entry in filesContext.ChangeTracker.Entries<FileInformation>().ToList())
        {
            if (!IsBeingDeleted(entry))
            {
                continue;
            }

            FileIntegrityRecord? record = filesContext.ChangeTracker.Entries<FileIntegrityRecord>()
                .Select(candidate => candidate.Entity)
                .SingleOrDefault(candidate => candidate.FileInformationId == entry.Entity.Id)
                ?? filesContext.FileIntegrityRecords.SingleOrDefault(candidate => candidate.FileInformationId == entry.Entity.Id);
            if (record is null)
            {
                continue;
            }

            record.HardDelete();
            _ = filesContext.FileIntegrityRecords.Remove(record);
        }
    }

    private static bool IsBeingDeleted(EntityEntry<FileInformation> entry) =>
        entry.State == EntityState.Deleted ||
        (entry.State == EntityState.Modified &&
         entry.Property(nameof(FileInformation.DeletedAt)).IsModified &&
         entry.Entity.DeletedAt is not null);
}
