using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Owner;

public static class KnowledgeOwnerReadModelProjector
{
    public static KnowledgeOwnerReadModel ToOwnerReadModel(this IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions, KnowledgeOwnerCorpusOption? selectedCorpus, IReadOnlyList<KnowledgeSource> sources) => new(corpusOptions, selectedCorpus, sources.Select(ToOwnerSourceOption).ToArray());

    public static IReadOnlyList<KnowledgeOwnerCorpusOption> ToOwnerCorpusOptions(this IReadOnlyList<KnowledgeCorpus> corpora) => corpora.Select(ToOwnerCorpusOption).ToArray();

    public static KnowledgeOwnerCorpusOption? ResolveSelectedOwnerCorpus(this IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions, Guid? selectedCorpusId) => corpusOptions.FirstOrDefault(option => selectedCorpusId is not null && option.CorpusId == selectedCorpusId.Value) ?? corpusOptions.FirstOrDefault();

    public static KnowledgeOwnerCorpusOption ToOwnerCorpusOption(this KnowledgeCorpus corpus) => new(string.IsNullOrWhiteSpace(corpus.Description) ? corpus.Name : $"{corpus.Name} — {corpus.Description}", $"{(corpus.IsActive ? "Enabled" : "Disabled")}{(string.IsNullOrWhiteSpace(corpus.Description) ? string.Empty : $" · {corpus.Description}")}") { CorpusId = corpus.Id };

    private static KnowledgeOwnerSourceOption ToOwnerSourceOption(this KnowledgeSource source) => new($"{source.Name} · {source.SourceType} · imported {source.ImportedAt:g}") { SourceId = source.Id };
}
