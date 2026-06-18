using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

internal static class KnowledgeSourceInputFormatImporterNames
{
    public static string ToImporterName(this KnowledgeSourceInputFormat format) => format switch
    {
        KnowledgeSourceInputFormat.Markdown => "markdown",
        KnowledgeSourceInputFormat.PlainText => "plain-text",
        KnowledgeSourceInputFormat.TechQaJson => "techqa-json",
        KnowledgeSourceInputFormat.RagEvaluationJson => "rag-eval-json",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Knowledge source input format is not supported.")
    };
}
