using System.Text.RegularExpressions;
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
            ExpectedChunkIds = question.ExpectedChunkIds.Count > 0
                ? question.ExpectedChunkIds
                : FindExpectedChunkIds(question.ExpectedContextTexts, chunks)
        })];

        return new KnowledgeImportResult { CorpusId = request.CorpusId, Sources = [source], Documents = documents, Chunks = chunks, Questions = questions };
    }

    private static IReadOnlyList<Guid> FindExpectedChunkIds(IReadOnlyList<string> expectedContextTexts, IReadOnlyList<KnowledgeChunk> chunks)
    {
        if (expectedContextTexts.Count == 0)
            return [];

        string[] normalizedContexts = [.. expectedContextTexts
            .Select(NormalizeExpectedContextText)
            .Where(static text => text.Length > 0)
            .Distinct(StringComparer.Ordinal)];

        if (normalizedContexts.Length == 0)
            return [];

        return [.. chunks
            .Where(chunk => ExpectedContextMatchesChunk(normalizedContexts, chunk.Text))
            .Select(static chunk => chunk.Id)
            .Distinct()];
    }

    private static bool ExpectedContextMatchesChunk(IReadOnlyList<string> normalizedContexts, string chunkText)
    {
        string normalizedChunkText = NormalizeExpectedContextText(chunkText);
        if (normalizedChunkText.Length == 0) return false;

        return normalizedContexts.Any(expectedContext =>
            normalizedChunkText.Contains(expectedContext, StringComparison.Ordinal) ||
            expectedContext.Contains(normalizedChunkText, StringComparison.Ordinal));
    }

    private static string NormalizeExpectedContextText(string text)
    {
        string withoutMarkup = Regex.Replace(text, @"[`*_>#\[\]()]", " ");
        return Regex.Replace(withoutMarkup.Trim().ToLowerInvariant(), @"\s+", " ");
    }
}
