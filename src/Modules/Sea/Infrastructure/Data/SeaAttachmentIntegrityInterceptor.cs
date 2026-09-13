using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Sea.Infrastructure.Data;

/// <summary>
/// Keeps the Sea-owned attachment integrity projection synchronized with the attachment write transaction.
/// </summary>
public sealed class SeaAttachmentIntegrityInterceptor(IRequestContext requestContext) : SaveChangesInterceptor
{
    private static readonly string[] ProjectedProperties =
    [
        nameof(Attachment.EventId),
        nameof(Attachment.Kind),
        nameof(Attachment.Content),
        nameof(Attachment.ContentType),
        nameof(Attachment.ContentThumbnail),
        nameof(Attachment.Size),
        nameof(Attachment.FileName)
    ];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Synchronize(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Synchronize(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Synchronize(DbContext? context)
    {
        if (context is not SeaDbContext seaContext)
        {
            return;
        }

        foreach (EntityEntry<Attachment> entry in seaContext.ChangeTracker.Entries<Attachment>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                AddRecord(seaContext, entry.Entity);
                continue;
            }

            if (IsBeingDeleted(entry))
            {
                RemoveRecord(seaContext, entry.Entity.Id);
                continue;
            }

            if (entry.State != EntityState.Modified || !ProjectedProperties.Any(property => entry.Property(property).IsModified))
            {
                continue;
            }

            SynchronizeChangedAttachment(seaContext, entry);
        }
    }

    private void AddRecord(SeaDbContext context, Attachment attachment)
    {
        string tenantId = requestContext.TenantId ?? throw new InvalidOperationException("Attachment integrity cannot be recorded without a tenant.");
        AttachmentIntegrityRecord record = AttachmentIntegrityRecord.Create(
            attachment.Id,
            tenantId,
            AttachmentIntegrityProjection.ComputeDigest(attachment));
        record.MarkVerified(DateTime.UtcNow);
        _ = context.AttachmentIntegrityRecords.Add(record);
    }

    private static void SynchronizeChangedAttachment(SeaDbContext context, EntityEntry<Attachment> entry)
    {
        AttachmentIntegrityRecord? record = context.ChangeTracker.Entries<AttachmentIntegrityRecord>()
            .Select(candidate => candidate.Entity)
            .SingleOrDefault(candidate => candidate.AttachmentId == entry.Entity.Id)
            ?? context.AttachmentIntegrityRecords.SingleOrDefault(candidate => candidate.AttachmentId == entry.Entity.Id);
        if (record is null || record.State != AttachmentIntegrityState.Verified ||
            !string.Equals(record.Digest, ComputeOriginalDigest(entry), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Attachment integrity is unavailable.");
        }

        record.ReplaceDigest(AttachmentIntegrityProjection.ComputeDigest(entry.Entity), DateTime.UtcNow);
    }

    private static bool IsBeingDeleted(EntityEntry<Attachment> entry) =>
        entry.State == EntityState.Deleted ||
        (entry.State == EntityState.Modified &&
         entry.Property(nameof(Attachment.DeletedAt)).IsModified &&
         entry.Entity.DeletedAt is not null);

    private static void RemoveRecord(SeaDbContext context, Guid attachmentId)
    {
        AttachmentIntegrityRecord? record = context.ChangeTracker.Entries<AttachmentIntegrityRecord>()
            .Select(candidate => candidate.Entity)
            .SingleOrDefault(candidate => candidate.AttachmentId == attachmentId)
            ?? context.AttachmentIntegrityRecords.SingleOrDefault(candidate => candidate.AttachmentId == attachmentId);
        if (record is null)
        {
            return;
        }

        record.HardDelete();
        _ = context.AttachmentIntegrityRecords.Remove(record);
    }

    private static string ComputeOriginalDigest(EntityEntry<Attachment> entry)
    {
        var result = Attachment.Create(
            entry.OriginalValues.GetValue<Guid>(nameof(Attachment.EventId)),
            entry.OriginalValues.GetValue<string>(nameof(Attachment.Content)),
            entry.OriginalValues.GetValue<string>(nameof(Attachment.ContentType)),
            entry.OriginalValues.GetValue<string?>(nameof(Attachment.ContentThumbnail)),
            entry.OriginalValues.GetValue<long>(nameof(Attachment.Size)),
            entry.OriginalValues.GetValue<AttachmentKind>(nameof(Attachment.Kind)),
            entry.OriginalValues.GetValue<string?>(nameof(Attachment.FileName)));
        if (result.IsFailure)
        {
            throw new InvalidOperationException("Attachment integrity cannot be calculated from the stored attachment.");
        }

        Attachment original = result.Value!;
        original.Id = entry.Entity.Id;
        return AttachmentIntegrityProjection.ComputeDigest(original);
    }
}
