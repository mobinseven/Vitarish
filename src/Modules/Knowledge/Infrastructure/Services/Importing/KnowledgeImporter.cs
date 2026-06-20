using MediatR;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

public sealed class KnowledgeImporter(IKnowledgeChunker chunker, IKnowledgeCorpusRepository corpusRepository, ISender sender) : IKnowledgeImporter
{
    private readonly KnowledgeImportContentReader contentReader = new();

    public async Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        KnowledgeImportResult result = await BuildImportResultAsync(null, request, cancellationToken);
        await sender.Send(new SaveKnowledgeImportCommand(result), cancellationToken);
        return result;
    }

    public async Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        KnowledgeImportResult result = await BuildImportResultAsync(sourceId, request, cancellationToken);
        await sender.Send(new ReplaceKnowledgeSourceImportCommand(sourceId, result), cancellationToken);
        return result;
    }

    private async Task<KnowledgeImportResult> BuildImportResultAsync(Guid? requestedSourceId, KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Format);
        KnowledgeCorpus? corpus = await corpusRepository.GetCorpusAsync(request.CorpusId, request.TenantId, cancellationToken);
        if (corpus is null)
            throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' does not exist.");
        if (!corpus.IsActive)
            throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is inactive.");
        if (!string.Equals(corpus.TenantId, request.TenantId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is not available for tenant '{request.TenantId}'.");

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
            OriginalLocation = request.OriginalLocation,
            Version = request.Version ?? request.ContentLength?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Checksum = request.Checksum,
            ImportedAt = DateTimeOffset.UtcNow
        };

        List<KnowledgeDocument> documents = [];
        List<KnowledgeChunk> chunks = [];
        Dictionary<string, List<KnowledgeChunk>> chunksByEvaluationItemKey = new(StringComparer.Ordinal);
        foreach (KnowledgeDocumentDraft draft in content.Documents)
        {
            Guid documentId = Guid.NewGuid();
            KnowledgeDocument document = new()
            {
                Id = documentId,
                CorpusId = request.CorpusId,
                TenantId = request.TenantId,
                SourceId = sourceId,
                Title = draft.Title ?? sourceName,
                Text = draft.Text,
                Product = draft.Product,
                Version = draft.Version,
                Language = draft.Language
            };
            documents.Add(document);

            IReadOnlyList<KnowledgeChunkDraft> chunkDrafts = chunker.Chunk(document);
            foreach (KnowledgeChunkDraft chunkDraft in chunkDrafts)
            {
                KnowledgeChunk chunk = new()
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
                };

                chunks.Add(chunk);

                if (!string.IsNullOrWhiteSpace(draft.EvaluationItemKey))
                {
                    if (!chunksByEvaluationItemKey.TryGetValue(draft.EvaluationItemKey, out List<KnowledgeChunk>? rowChunks))
                    {
                        rowChunks = [];
                        chunksByEvaluationItemKey[draft.EvaluationItemKey] = rowChunks;
                    }

                    rowChunks.Add(chunk);
                }
            }
        }

        KnowledgeQuestion[] questions = [.. content.Questions.Select(question =>
        {
            IReadOnlyList<Guid> expectedChunkIds =
                !string.IsNullOrWhiteSpace(question.EvaluationItemKey)
                && chunksByEvaluationItemKey.TryGetValue(question.EvaluationItemKey, out List<KnowledgeChunk>? rowChunks)
                    ? [.. rowChunks.Select(static chunk => chunk.Id)]
                    : FindExpectedChunkIds(question.ExpectedContextTexts, chunks);

            return new KnowledgeQuestion
            {
                Id = Guid.NewGuid(),
                CorpusId = request.CorpusId,
                Question = question.Question,
                ExpectedAnswer = question.ExpectedAnswer,
                ExpectedChunkIds = expectedChunkIds
            };
        })];

        return new KnowledgeImportResult { CorpusId = request.CorpusId, Sources = [source], Documents = documents, Chunks = chunks, Questions = questions };
    }

    private static IReadOnlyList<Guid> FindExpectedChunkIds(IReadOnlyList<string> expectedContextTexts, IReadOnlyList<KnowledgeChunk> chunks)
    {
        if (expectedContextTexts.Count == 0)
            return [];

        string[] trimmedContexts = [.. expectedContextTexts
            .Select(static t => t.Trim())
            .Where(static t => t.Length > 0)];

        if (trimmedContexts.Length == 0)
            return [];

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
