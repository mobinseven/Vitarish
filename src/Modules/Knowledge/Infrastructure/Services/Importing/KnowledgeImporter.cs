using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

public sealed class KnowledgeImporter(IKnowledgeChunker chunker, IKnowledgeRepository repository) : IKnowledgeImporter
{
    private static readonly HashSet<string> TextFormats = new(StringComparer.OrdinalIgnoreCase) { "plain-text", "markdown" };

    public async Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Format);
        KnowledgeCorpus? corpus = await repository.GetCorpusAsync(request.CorpusId, request.TenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is inactive.");
        if (!string.Equals(corpus.TenantId, request.TenantId, StringComparison.Ordinal)) throw new InvalidOperationException($"Knowledge corpus '{request.CorpusId}' is not available for tenant '{request.TenantId}'.");
        if (!TextFormats.Contains(request.Format)) throw new NotSupportedException($"Knowledge import format '{request.Format}' is not supported by the MVP importer.");

        using StreamReader reader = new(request.Content, leaveOpen: true);
        string text = await reader.ReadToEndAsync(cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        string sourceId = Guid.NewGuid().ToString("n");
        string documentId = Guid.NewGuid().ToString("n");
        KnowledgeSource source = new()
        {
            Id = sourceId,
            CorpusId = request.CorpusId,
            TenantId = request.TenantId,
            Name = request.SourceName ?? request.Format,
            SourceType = request.Format,
            ImportedAt = DateTimeOffset.UtcNow
        };
        KnowledgeDocument document = new()
        {
            Id = documentId,
            CorpusId = request.CorpusId,
            TenantId = request.TenantId,
            SourceId = sourceId,
            Title = source.Name,
            Text = text
        };
        KnowledgeChunk[] chunks = [.. chunker.Chunk(document).Select(draft => new KnowledgeChunk
        {
            Id = Guid.NewGuid().ToString("n"),
            CorpusId = request.CorpusId,
            TenantId = request.TenantId,
            SourceId = sourceId,
            DocumentId = documentId,
            Text = draft.Text,
            Order = draft.Order,
            Title = draft.Title,
            HeadingPath = draft.HeadingPath
        })];
        KnowledgeImportResult result = new() { CorpusId = request.CorpusId, Sources = [source], Documents = [document], Chunks = chunks };
        await repository.SaveImportAsync(result, cancellationToken);
        return result;
    }
}
