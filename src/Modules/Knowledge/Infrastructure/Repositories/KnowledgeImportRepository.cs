using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeImportRepository(KnowledgeDbContext dbContext)
    : EntityRepository<KnowledgeSourceEntity, KnowledgeSourceDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeSourceEntity, KnowledgeSourceDescriptor>,
      IKnowledgeImportRepository
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    public async Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken)
    {
        KnowledgeImportRules.ValidateImport(result);
        string tenantId = result.Sources[0].TenantId;
        _ = await RequireCorpusAsync(result.CorpusId, tenantId, cancellationToken);
        StageImport(result);
        _ = await DbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceSourceAsync(Guid sourceId, KnowledgeImportResult result, CancellationToken cancellationToken)
    {
        KnowledgeImportRules.ValidateImport(result);
        if (result.Sources.Count != 1) throw new InvalidOperationException("Source replacement must contain exactly one source.");
        KnowledgeSource source = result.Sources[0];
        if (source.Id != sourceId) throw new InvalidOperationException($"Replacement source id '{source.Id}' must match requested source id '{sourceId}'.");

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            KnowledgeSourceEntity existing = await DbContext.Sources.FirstOrDefaultAsync(s => s.Id == sourceId && s.CorpusId == result.CorpusId && s.TenantId == source.TenantId, cancellationToken)
                ?? throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{result.CorpusId}' for tenant '{source.TenantId}'.");
            Guid[] existingChunkIds = await DbContext.Chunks.AsNoTracking().Where(c => c.SourceId == sourceId && c.CorpusId == result.CorpusId && c.TenantId == source.TenantId).Select(c => c.Id).ToArrayAsync(cancellationToken);
            DbContext.Sources.Remove(existing);
            await KnowledgeQuestionRemover.RemoveQuestionsForChunksAsync(DbContext, result.CorpusId, existingChunkIds, cancellationToken);
            _ = await DbContext.SaveChangesAsync(cancellationToken);

            StageImport(result);
            _ = await DbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<KnowledgeCorpusEntity> RequireCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await DbContext.Corpora.FirstOrDefaultAsync(c => c.Id == corpusId && c.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist for tenant '{tenantId}'.");

    private void StageImport(KnowledgeImportResult result)
    {
        DbContext.Sources.AddRange(result.Sources.Select(source => new KnowledgeSourceEntity { Id = source.Id, CorpusId = source.CorpusId, TenantId = source.TenantId, Name = source.Name, SourceType = source.SourceType, OriginalLocation = source.OriginalLocation, Version = source.Version, Checksum = source.Checksum, ImportedAt = source.ImportedAt }));
        DbContext.Documents.AddRange(result.Documents.Select(document => new KnowledgeDocumentEntity { Id = document.Id, CorpusId = document.CorpusId, TenantId = document.TenantId, SourceId = document.SourceId, Title = document.Title, Text = document.Text, Product = document.Product, Version = document.Version, Language = document.Language }));
        DbContext.Chunks.AddRange(result.Chunks.Select(chunk => new KnowledgeChunkEntity { Id = chunk.Id, CorpusId = chunk.CorpusId, SourceId = chunk.SourceId, DocumentId = chunk.DocumentId, TenantId = chunk.TenantId, Text = chunk.Text, Order = chunk.Order, Title = chunk.Title, HeadingPath = chunk.HeadingPath }));
        DbContext.Questions.AddRange(result.Questions.Select(question => new KnowledgeQuestionEntity { Id = question.Id, CorpusId = question.CorpusId, Question = question.Question, ExpectedAnswer = question.ExpectedAnswer, ExpectedChunkIdsJson = JsonSerializer.Serialize(question.ExpectedChunkIds, KnowledgeImportRules.JsonOptions) }));
    }
}
