using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Entities;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

public sealed record KnowledgeOwnerSourceFileProjection(KnowledgeDocumentEntity Document, int ChunkCount, DateTimeOffset? IndexedAt);

public static class KnowledgeOwnerCollectionProjector
{
    public static KnowledgeOwnerCollectionReadModel ToOwnerCollection(
        IReadOnlyList<KnowledgeCorpusEntity> corpora,
        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>> sourcesByCorpus,
        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>> filesBySource,
        Guid? selectedCorpusId,
        Guid? selectedSourceId)
    {
        KnowledgeOwnerCorpusCollectionItem[] corpusItems = corpora.Select(corpus => corpus.ToCorpusItem(sourcesByCorpus, filesBySource)).ToArray();
        Guid? resolvedCorpusId = corpusItems.Any(corpus => corpus.CorpusId == selectedCorpusId) ? selectedCorpusId : corpusItems.FirstOrDefault()?.CorpusId;
        Guid? resolvedSourceId = ResolveSelectedSourceId(corpusItems, resolvedCorpusId, selectedSourceId);
        return new KnowledgeOwnerCollectionReadModel(corpusItems, resolvedCorpusId, resolvedSourceId);
    }

    public static KnowledgeOwnerCorpusOption ToOwnerCorpusOption(this KnowledgeOwnerCorpusCollectionItem corpus)
        => new(corpus.CorpusName, $"{(corpus.IsActive ? "Enabled" : "Disabled")}{(string.IsNullOrWhiteSpace(corpus.CorpusDescription) ? string.Empty : $" · {corpus.CorpusDescription}")}") { CorpusId = corpus.CorpusId };

    public static KnowledgeOwnerSourceOption ToOwnerSourceOption(this KnowledgeOwnerSourceCollectionItem source)
        => new($"{source.SourceName} · {source.SourceType} · imported {source.ImportedAt:g}") { SourceId = source.SourceId };

    private static KnowledgeOwnerCorpusCollectionItem ToCorpusItem(
        this KnowledgeCorpusEntity corpus,
        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>> sourcesByCorpus,
        IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>> filesBySource)
    {
        IReadOnlyList<KnowledgeSourceEntity> sources = sourcesByCorpus.TryGetValue(corpus.Id, out IReadOnlyList<KnowledgeSourceEntity>? value) ? value : [];
        return new KnowledgeOwnerCorpusCollectionItem(corpus.Id, corpus.Name, corpus.Description, corpus.IsActive, sources.Select(source => source.ToSourceItem(filesBySource)).ToArray());
    }

    private static KnowledgeOwnerSourceCollectionItem ToSourceItem(this KnowledgeSourceEntity source, IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>> filesBySource)
    {
        IReadOnlyList<KnowledgeOwnerSourceFileProjection> files = filesBySource.TryGetValue(source.Id, out IReadOnlyList<KnowledgeOwnerSourceFileProjection>? value) ? value : [];
        return new KnowledgeOwnerSourceCollectionItem(source.Id, source.Name, source.SourceType, source.ImportedAt, source.OriginalLocation, files.Select(ToFileItem).ToArray());
    }

    private static KnowledgeOwnerSourceFileCollectionItem ToFileItem(KnowledgeOwnerSourceFileProjection file)
        => new(file.Document.Id, file.Document.Title, file.Document.Language, file.Document.Text.Length, file.ChunkCount, file.IndexedAt);

    private static Guid? ResolveSelectedSourceId(IReadOnlyList<KnowledgeOwnerCorpusCollectionItem> corpora, Guid? selectedCorpusId, Guid? selectedSourceId)
    {
        KnowledgeOwnerCorpusCollectionItem? corpus = corpora.FirstOrDefault(item => item.CorpusId == selectedCorpusId);
        if (corpus is null) return null;
        return corpus.Sources.Any(source => source.SourceId == selectedSourceId) ? selectedSourceId : corpus.Sources.FirstOrDefault()?.SourceId;
    }
}
