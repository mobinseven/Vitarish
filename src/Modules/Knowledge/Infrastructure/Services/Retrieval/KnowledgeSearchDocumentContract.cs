using Vitarish.Knowledge.Application.Abstractions;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chunk.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(chunk.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chunk.TenantId);

        if (!string.Equals(chunk.CorpusId, corpus.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' does not belong to corpus '{corpus.Id}'.");
        }

        if (!string.Equals(chunk.TenantId, corpus.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' tenant does not match corpus '{corpus.Id}' tenant.");
        }

        return new SearchDocument
        {
            Id = chunk.Id,
            Title = string.IsNullOrWhiteSpace(chunk.HeadingPath)
                ? chunk.Title ?? chunk.DocumentId ?? chunk.Id
                : $"{chunk.Title ?? chunk.DocumentId ?? chunk.Id} — {chunk.HeadingPath}",
            Body = chunk.Text,
            TenantId = corpus.TenantId,
            Type = DocumentType,
            IndexName = IndexName,
            Metadata = new Dictionary<string, object?>
            {
                [CorpusIdMetadataKey] = chunk.CorpusId,
                [SourceIdMetadataKey] = chunk.SourceId,
                [DocumentIdMetadataKey] = chunk.DocumentId,
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
            ChunkId = document.Id,
            SourceId = MetadataString(document, SourceIdMetadataKey),
            DocumentId = MetadataString(document, DocumentIdMetadataKey),
            Text = document.Body ?? document.Title,
            Title = document.Title,
            Score = document.Score ?? 0
        };
    }

    private static string MetadataString(SearchDocument document, string key) => document.Metadata.TryGetValue(key, out object? found) ? found?.ToString() ?? string.Empty : string.Empty;
}
