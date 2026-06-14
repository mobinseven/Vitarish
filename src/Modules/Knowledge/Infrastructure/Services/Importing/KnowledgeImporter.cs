using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

public sealed class KnowledgeImporter(IKnowledgeChunker chunker, IKnowledgeCorpusRepository corpusRepository, IKnowledgeImportRepository importRepository) : IKnowledgeImporter
{
    private readonly KnowledgeImportContentReader contentReader = new();

    public async Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        KnowledgeImportResult result = await BuildImportResultAsync(null, request, cancellationToken);
        await importRepository.SaveImportAsync(result, cancellationToken);
        return result;
    }

    public async Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
                KnowledgeImportResult result = await BuildImportResultAsync(sourceId, request, cancellationToken);
        await importRepository.ReplaceSourceAsync(sourceId, result, cancellationToken);
        return result;
    }

    private async Task<KnowledgeImportResult> BuildImportResultAsync(Guid? requestedSourceId, KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
                ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Format);
        KnowledgeCorpus? corpus = await corpusRepository.GetCorpusAsync(request.CorpusId, request.TenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is inactive.");
        if (!string.Equals(corpus.TenantId, request.TenantId, StringComparison.Ordinal)) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is not available for tenant '{request.TenantId}'.");

        string sourceName = request.SourceName ?? request.Format;
        KnowledgeImportContent content = await contentReader.ReadAsync(request.Content, request.Format, sourceName, cancellationToken);

        Guid sourceId = requestedSourceId ?? Guid.NewGuid();
        KnowledgeSource source = new()
        {
            Id = sourceId,
            CorpusId = request.CorpusId,
            TenantId = request.TenantId,
            Name = sourceName,
            SourceType = request.Format,
            ImportedAt = DateTimeOffset.UtcNow
        };

        List<KnowledgeDocument> documents = [];
        List<KnowledgeChunk> chunks = [];
        foreach (KnowledgeDocumentDraft draft in content.Documents)
        {
            Guid documentId = Guid.NewGuid();
            KnowledgeDocument document = new()
            {
                Id = documentId,
                CorpusId = request.CorpusId,
                TenantId = request.TenantId,
                SourceId = sourceId,
                Title = draft.Title,
                Text = draft.Text,
                Product = draft.Product,
                Version = draft.Version,
                Language = draft.Language
            };
            documents.Add(document);
            chunks.AddRange(chunker.Chunk(document).Select(chunkDraft => new KnowledgeChunk
            {
                Id = Guid.NewGuid(),
                CorpusId = request.CorpusId,
                TenantId = request.TenantId,
                SourceId = sourceId,
                DocumentId = documentId,
                Text = chunkDraft.Text,
                Order = chunkDraft.Order,
                Title = chunkDraft.Title,
                HeadingPath = chunkDraft.HeadingPath
            }));
        }

        KnowledgeQuestion[] questions = [.. content.Questions.Select(question => new KnowledgeQuestion
        {
            Id = Guid.NewGuid(),
            CorpusId = request.CorpusId,
            Question = question.Question,
            ExpectedAnswer = question.ExpectedAnswer,
            ExpectedChunkIds = FindExpectedChunkIds(question.ExpectedContextTexts, chunks)
        })];

        return new KnowledgeImportResult { CorpusId = request.CorpusId, Sources = [source], Documents = documents, Chunks = chunks, Questions = questions };
    }

    private static IReadOnlyList<Guid> FindExpectedChunkIds(IReadOnlyList<string> expectedContextTexts, IReadOnlyList<KnowledgeChunk> chunks)
    {
        if (expectedContextTexts.Count == 0) return [];

        string[] trimmedContexts = [.. expectedContextTexts
            .Select(static t => t.Trim())
            .Where(static t => t.Length > 0)];

        if (trimmedContexts.Length == 0) return [];

        return [.. chunks
            .Where(chunk =>
            {
                string chunkText = chunk.Text.Trim();
                if (chunkText.Length == 0) return false;

                return trimmedContexts.Any(expectedContext =>
                    chunkText.Contains(expectedContext, StringComparison.OrdinalIgnoreCase) ||
                    expectedContext.Contains(chunkText, StringComparison.OrdinalIgnoreCase));
            })
            .Select(static chunk => chunk.Id)
            .Distinct()];
    }
}
