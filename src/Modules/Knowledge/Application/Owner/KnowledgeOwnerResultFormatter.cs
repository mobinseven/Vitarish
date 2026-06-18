using System.Globalization;
using Vitarish.Knowledge.Application.Abstractions;

namespace Vitarish.Knowledge.Application.Owner;

public static class KnowledgeOwnerResultFormatter
{
    public static string ToOwnerImportSummary(this KnowledgeImportResult result) => $"Imported {result.Sources.Count} source(s), {result.Documents.Count} document(s), and {result.Chunks.Count} searchable section(s).";

    public static string ToOwnerIndexSummary(this KnowledgeIndexResult result) => $"Indexed {result.IndexedChunkCount} searchable section(s).";

    public static string ToOwnerEvaluationSummary(this KnowledgeEvaluationRun run) => string.Create(CultureInfo.InvariantCulture, $"recall@5 {run.RecallAt5:0.###} · recall@10 {run.RecallAt10:0.###} · average expected rank {run.AverageExpectedChunkRank:0.###}");
}
