using System.Globalization;
using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Infrastructure.Extensions;

public static class KnowledgeOwnerResultFormatter
{
    public static string ToOwnerImportSummary(this KnowledgeImportResult result) => $"Imported {result.Sources.Count} source(s), {result.Documents.Count} document(s), and {result.Chunks.Count} searchable section(s).";

    public static string ToOwnerIndexSummary(this KnowledgeIndexResult result) => $"Indexed {result.IndexedChunkCount} searchable section(s).";

    public static string ToOwnerEvaluationSummary(this KnowledgeEvaluationRun run) => string.Create(CultureInfo.InvariantCulture, $"recall@5 {run.RecallAt5:0.###} · recall@10 {run.RecallAt10:0.###} · average expected rank {run.AverageExpectedChunkRank:0.###}");

    public static string ToOwnerEvaluationItemReport(this KnowledgeEvaluationItem item) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"""
            Question ID: {item.QuestionId}
            Gold evidence mapped: {YesNo(item.HasExpectedContext)}
            Gold evidence found in top 5: {YesNo(item.ExpectedContextFoundInTop5)}
            Gold evidence found in top 10: {YesNo(item.ExpectedContextFoundInTop10)}
            Gold evidence found in configured TopK: {YesNo(item.ExpectedContextWasRetrieved)}
            Answer produced: {YesNo(item.WasAnswered)}
            Answer correctness: {AnswerCorrectness(item.GeneratedAnswerMatchedExpectedAnswer)}
            Generated answer: {item.GeneratedAnswer}
            """);

    private static string YesNo(bool value) => value ? "yes" : "no";

    private static string AnswerCorrectness(bool? value) => value switch
    {
        true => "yes",
        false => "no",
        null => "not scored"
    };
}
