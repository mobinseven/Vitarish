using System.Text.Json;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

internal static class KnowledgeImportRules
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void ValidateImport(KnowledgeImportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Sources.Count == 0) throw new InvalidOperationException("Knowledge import must contain at least one source.");
        string tenantId = result.Sources[0].TenantId;
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        HashSet<Guid> sourceIds = [.. result.Sources.Select(source => source.Id)];
        HashSet<Guid> documentIds = [.. result.Documents.Select(document => document.Id)];
        foreach (KnowledgeSource source in result.Sources)
        {
            ValidateTenant(source.TenantId, tenantId);
            ValidateCorpus(source.CorpusId, result.CorpusId, "source");
        }
        foreach (KnowledgeDocument document in result.Documents)
        {
            ValidateTenant(document.TenantId, tenantId);
            ValidateCorpus(document.CorpusId, result.CorpusId, "document");
            if (!sourceIds.Contains(document.SourceId)) throw new InvalidOperationException($"Imported document '{document.Id}' references source '{document.SourceId}' that is not in the import.");
        }
        foreach (KnowledgeChunk chunk in result.Chunks)
        {
            ValidateTenant(chunk.TenantId, tenantId);
            ValidateCorpus(chunk.CorpusId, result.CorpusId, "chunk");
            if (!sourceIds.Contains(chunk.SourceId)) throw new InvalidOperationException($"Imported chunk '{chunk.Id}' references source '{chunk.SourceId}' that is not in the import.");
            if (!documentIds.Contains(chunk.DocumentId)) throw new InvalidOperationException($"Imported chunk '{chunk.Id}' references document '{chunk.DocumentId}' that is not in the import.");
        }
        foreach (KnowledgeQuestion question in result.Questions)
        {
            ValidateCorpus(question.CorpusId, result.CorpusId, "question");
        }
    }

    private static void ValidateTenant(string actual, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actual);
        if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw new InvalidOperationException("Imported tenant-bearing items must share one tenant ID.");
    }

    private static void ValidateCorpus(Guid actual, Guid expected, string item)
    {
        if (actual != expected) throw new InvalidOperationException($"Imported {item} references corpus '{actual}' instead of '{expected}'.");
    }
}
