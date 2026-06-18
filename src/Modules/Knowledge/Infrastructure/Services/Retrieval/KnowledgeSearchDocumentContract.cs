using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Retrieval;

/// <summary>
/// Defines the search-document contract for knowledge chunks. Corpus tenant scope is the
/// single tenant source of truth for indexed knowledge documents.
/// </summary>
public static class KnowledgeSearchDocumentContract
{
    public const string IndexName = "knowledge-chunks";
    public const string DocumentType = "knowledge-chunk";

    public const string CorpusIdMetadataKey = "corpusId";
    public const string SourceIdMetadataKey = "sourceId";
    public const string DocumentIdMetadataKey = "documentId";
    public const string HeadingPathMetadataKey = "headingPath";
    public const string OrderMetadataKey = "order";

    public static SearchDocument ToSearchDocument(KnowledgeCorpus corpus, KnowledgeChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.TenantId);

        if (chunk.CorpusId != corpus.Id)
        {
            throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' does not belong to corpus '{corpus.Id}'.");
        }

        if (!string.Equals(chunk.TenantId, corpus.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' tenant does not match corpus '{corpus.Id}' tenant.");
        }

        string title = string.IsNullOrWhiteSpace(chunk.Title) ? chunk.DocumentId.ToString("N") : chunk.Title;

        return new SearchDocument
        {
            Id = chunk.Id.ToString("N"),
            Title = string.IsNullOrWhiteSpace(chunk.HeadingPath)
                ? title
                : $"{title} — {chunk.HeadingPath}",
            Body = chunk.Text,
            TenantId = corpus.TenantId,
            Type = DocumentType,
            IndexName = IndexName,
            Metadata = new Dictionary<string, object?>
            {
                [CorpusIdMetadataKey] = chunk.CorpusId.ToString("N"),
                [SourceIdMetadataKey] = chunk.SourceId.ToString("N"),
                [DocumentIdMetadataKey] = chunk.DocumentId.ToString("N"),
                [HeadingPathMetadataKey] = chunk.HeadingPath,
                [OrderMetadataKey] = chunk.Order
            }
        };
    }

    public static KnowledgeHit ToKnowledgeHit(SearchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new KnowledgeHit
        {
            ChunkId = Guid.Parse(document.Id),
            SourceId = MetadataGuid(document, SourceIdMetadataKey),
            DocumentId = MetadataGuid(document, DocumentIdMetadataKey),
            Text = document.Body ?? document.Title,
            Title = document.Title,
            Score = document.Score ?? 0
        };
    }

    private static Guid MetadataGuid(SearchDocument document, string key) => document.Metadata.TryGetValue(key, out object? found) && Guid.TryParse(found?.ToString(), out Guid value) ? value : Guid.Empty;
}
