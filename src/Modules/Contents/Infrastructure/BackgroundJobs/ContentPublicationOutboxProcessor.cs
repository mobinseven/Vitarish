using System.Runtime.CompilerServices;
using Hangfire;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Infrastructure.Data;
using Vitarish.Contents.Infrastructure.SearchProviders;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Contents.Infrastructure.BackgroundJobs;

public sealed class ContentPublicationOutboxProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<ContentPublicationOutboxProcessor> logger)
{
    internal const string JobId = "contents:publication-outbox";
    internal const string BackgroundResource = JobId;

    private const int BatchSize = 20;
    private const int MaximumAttempts = 10;
    private const string InterruptedAttemptLastError = "A leased processing attempt may have been interrupted.";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public Task ProcessAsync(CancellationToken cancellationToken = default) =>
        tenantBackgroundExecutor.ExecutePerTenantAsync(BackgroundResource, ProcessTenantAsync, cancellationToken);

    private async Task ProcessTenantAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ContentsDbContext dbContext = serviceProvider.GetRequiredService<ContentsDbContext>();
        IRequestContext requestContext = serviceProvider.GetRequiredService<IRequestContext>();
        ISearchService searchService = serviceProvider.GetRequiredService<ISearchService>();
        ContentItemsSearchProvider searchProvider = serviceProvider.GetRequiredService<ContentItemsSearchProvider>();
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("A resolved tenant is required to process content publication records.");

        IReadOnlyList<ContentPublicationOutboxRecord> records = await LeaseBatchAsync(dbContext, tenantId, cancellationToken);
        foreach (ContentPublicationOutboxRecord record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guid recordId = record.Id;
            int leaseAttempt = record.Attempts;
            dbContext.ChangeTracker.Clear();
            await ProcessRecordAsync(dbContext, searchService, searchProvider, tenantId, recordId, leaseAttempt, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ContentPublicationOutboxRecord>> LeaseBatchAsync(
        ContentsDbContext dbContext,
        string tenantId,
        CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(ContentPublicationOutboxRecord))
            ?? throw new InvalidOperationException("Content publication outbox mapping is unavailable.");
        StoreObjectIdentifier table = StoreObjectIdentifier.Table(
            entityType.GetTableName() ?? throw new InvalidOperationException("Content publication outbox table mapping is unavailable."),
            entityType.GetSchema());
        ISqlGenerationHelper sql = dbContext.GetService<ISqlGenerationHelper>();
        string tableName = sql.DelimitIdentifier(table.Name, table.Schema);
        string Column(string propertyName) => sql.DelimitIdentifier(
            entityType.FindProperty(propertyName)?.GetColumnName(table)
                ?? throw new InvalidOperationException($"Content publication outbox property '{propertyName}' is not mapped."));

        string tenantColumn = Column(nameof(ContentPublicationOutboxRecord.TenantId));
        string completedColumn = Column(nameof(ContentPublicationOutboxRecord.CompletedAt));
        string attemptsColumn = Column(nameof(ContentPublicationOutboxRecord.Attempts));
        string nextAttemptColumn = Column(nameof(ContentPublicationOutboxRecord.NextAttemptAt));
        string leaseColumn = Column(nameof(ContentPublicationOutboxRecord.LeaseUntil));
        string createdColumn = Column(nameof(ContentPublicationOutboxRecord.CreatedAt));
        string idColumn = Column(nameof(ContentPublicationOutboxRecord.Id));

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        FormattableString query = FormattableStringFactory.Create(
            $"SELECT * FROM {tableName} WHERE {tenantColumn} = {{0}} AND {completedColumn} IS NULL AND {attemptsColumn} < {{1}} AND {nextAttemptColumn} <= {{2}} AND ({leaseColumn} IS NULL OR {leaseColumn} <= {{2}}) ORDER BY {createdColumn}, {idColumn} LIMIT {{3}} FOR UPDATE SKIP LOCKED",
            tenantId,
            MaximumAttempts,
            now,
            BatchSize);
        List<ContentPublicationOutboxRecord> records = await dbContext.ContentPublicationOutbox
            .FromSqlInterpolated(query)
            .ToListAsync(cancellationToken);

        foreach (ContentPublicationOutboxRecord record in records)
        {
            record.Attempts++;
            record.LeaseUntil = now + LeaseDuration;
            record.LastError = InterruptedAttemptLastError;
        }

        if (records.Count > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return records;
    }

    private async Task ProcessRecordAsync(
        ContentsDbContext dbContext,
        ISearchService searchService,
        ContentItemsSearchProvider searchProvider,
        string tenantId,
        Guid recordId,
        int leaseAttempt,
        CancellationToken cancellationToken)
    {
        try
        {
            await SynchronizeRecordAsync(dbContext, searchService, searchProvider, tenantId, recordId, leaseAttempt, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            dbContext.ChangeTracker.Clear();
            await RecordFailureAsync(dbContext, tenantId, recordId, leaseAttempt, exception, cancellationToken);
        }
    }

    private async Task SynchronizeRecordAsync(
        ContentsDbContext dbContext,
        ISearchService searchService,
        ContentItemsSearchProvider searchProvider,
        string tenantId,
        Guid recordId,
        int leaseAttempt,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        ContentPublicationOutboxRecord? record = await dbContext.ContentPublicationOutbox.SingleOrDefaultAsync(
            entry => entry.Id == recordId && entry.TenantId == tenantId,
            cancellationToken);
        if (record is null || record.Attempts != leaseAttempt)
        {
            return;
        }

        await ContentPublicationLocks.AcquireAsync(dbContext, tenantId, record.ContentId, cancellationToken);
        await dbContext.Entry(record).ReloadAsync(cancellationToken);
        if (dbContext.Entry(record).State == EntityState.Detached ||
            record.CompletedAt is not null ||
            record.Attempts != leaseAttempt ||
            record.LeaseUntil is null || record.LeaseUntil <= DateTime.UtcNow ||
            record.NextAttemptAt > DateTime.UtcNow)
        {
            return;
        }

        // Recompute the current effect so a delayed Index/Remove record cannot overwrite newer publication state.
        SearchDocument? currentPublishedDocument = await searchProvider.GetPublishedDocumentAsync(
            record.ContentId,
            tenantId,
            cancellationToken);

        if (currentPublishedDocument is not null)
        {
            await searchService.IndexAsync(ContentItemsSearchProvider.IndexNameValue, [currentPublishedDocument], cancellationToken);
        }
        else
        {
            await searchService.DeleteAsync(
                ContentItemsSearchProvider.IndexNameValue,
                new SearchDeleteOptions
                {
                    TenantId = tenantId,
                    MetadataFilters = new Dictionary<string, string>
                    {
                        [ContentItemsSearchProvider.ContentIdMetadataKey] = record.ContentId.ToString()
                    }
                },
                cancellationToken);
        }

        record.CompletedAt = DateTime.UtcNow;
        record.LeaseUntil = null;
        record.LastError = null;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RecordFailureAsync(
        ContentsDbContext dbContext,
        string tenantId,
        Guid recordId,
        int leaseAttempt,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        ContentPublicationOutboxRecord? record = await dbContext.ContentPublicationOutbox.SingleOrDefaultAsync(
            entry => entry.Id == recordId && entry.TenantId == tenantId,
            cancellationToken);
        if (record is null || record.CompletedAt is not null || record.Attempts != leaseAttempt)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning(
                "Failure for content publication outbox record {OutboxId} ({ErrorType}) was superseded by completion, deletion, or a newer lease.",
                recordId,
                exception.GetType().Name);
            return;
        }

        record.LeaseUntil = null;
        record.LastError = LastError(exception, record.Operation, record.ContentId);
        if (leaseAttempt < MaximumAttempts)
        {
            int exponent = Math.Min(leaseAttempt - 1, MaximumAttempts - 1);
            record.NextAttemptAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, exponent));
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogError(
            "Content publication outbox record {OutboxId} for content {ContentId} operation {Operation} failed on attempt {Attempt}/{MaximumAttempts} ({ErrorType}).",
            recordId,
            record.ContentId,
            record.Operation,
            record.Attempts,
            MaximumAttempts,
            exception.GetType().Name);
    }

    private static string LastError(Exception exception, ContentPublicationOperation operation, Guid contentId)
    {
        string message = $"{exception.GetType().Name} while processing {operation} for content {contentId}.";
        return message.Length <= 1000 ? message : message[..1000];
    }
}
