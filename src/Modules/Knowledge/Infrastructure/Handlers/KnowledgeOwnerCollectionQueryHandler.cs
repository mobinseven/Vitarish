using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Queries;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class KnowledgeOwnerCollectionQueryHandler(KnowledgeDbContext dbContext) : IRequestHandler<KnowledgeOwnerCollectionQuery, KnowledgeOwnerCollectionReadModel>
{
    public async Task<KnowledgeOwnerCollectionReadModel> Handle(KnowledgeOwnerCollectionQuery request, CancellationToken cancellationToken)
    {
        string? tenantError = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId);
        if (tenantError is not null || string.IsNullOrWhiteSpace(request.OwnerId))
        {
            return KnowledgeOwnerCollectionReadModel.Empty;
        }

        string tenantId = request.TenantId!;
        KnowledgeCorpusEntity[] corpora = await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.TenantId == tenantId)
            .OrderBy(corpus => corpus.Name)
            .ToArrayAsync(cancellationToken);

        if (corpora.Length == 0)
        {
            return KnowledgeOwnerCollectionReadModel.Empty;
        }

        Guid[] corpusIds = corpora.Select(corpus => corpus.Id).ToArray();
        KnowledgeSourceEntity[] sources = await dbContext.Sources.AsNoTracking()
            .Where(source => source.TenantId == tenantId && corpusIds.Contains(source.CorpusId))
            .OrderBy(source => source.Name)
            .ThenBy(source => source.ImportedAt)
            .ToArrayAsync(cancellationToken);

        Guid[] sourceIds = sources.Select(source => source.Id).ToArray();
        KnowledgeDocumentEntity[] documents = sourceIds.Length == 0
            ? []
            : await dbContext.Documents.AsNoTracking()
                .Where(document => document.TenantId == tenantId && sourceIds.Contains(document.SourceId))
                .OrderBy(document => document.Title)
                .ToArrayAsync(cancellationToken);

        Guid[] documentIds = documents.Select(document => document.Id).ToArray();
        Dictionary<Guid, int> chunkCounts = documents.Length == 0
            ? []
            : await dbContext.Chunks.AsNoTracking()
                .Where(chunk => chunk.TenantId == tenantId && documentIds.Contains(chunk.DocumentId))
                .GroupBy(chunk => chunk.DocumentId)
                .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>> sourcesByCorpus = sources
            .GroupBy(source => source.CorpusId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<KnowledgeSourceEntity>)group.ToArray());

        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>> filesBySource = documents
            .GroupBy(document => document.SourceId)
            .ToDictionary<IGrouping<Guid, KnowledgeDocumentEntity>, Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>>(
                group => group.Key,
                group => group.Select(document => new KnowledgeOwnerSourceFileProjection(document, chunkCounts.GetValueOrDefault(document.Id), null)).ToArray());

        return KnowledgeOwnerCollectionProjector.ToOwnerCollection(corpora, sourcesByCorpus, filesBySource, request.SelectedCorpusId, request.FocusedSourceId);
    }
}
