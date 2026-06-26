using Vitarish.Knowledge.Contracts.Enums;

namespace Vitarish.Knowledge.Interface.Components;

internal static class KnowledgeSourceInputFormatLabels
{
    public static IReadOnlyList<KnowledgeSourceInputFormat> Formats { get; } = Enum.GetValues<KnowledgeSourceInputFormat>();

    public static string Label(this KnowledgeSourceInputFormat format) => format switch
    {
        KnowledgeSourceInputFormat.Markdown => "Markdown",
        KnowledgeSourceInputFormat.PlainText => "Plain text",
        KnowledgeSourceInputFormat.TechQaJson => "TechQA JSON",
        KnowledgeSourceInputFormat.RagEvaluationJson => "RAG evaluation JSON",
        KnowledgeSourceInputFormat.PlainQaJson => "Plain QA JSON",
        _ => format.ToString()
    };
}
