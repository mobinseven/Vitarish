using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Owner;

public sealed class KnowledgeOwnerReadModelProjector
{
    public KnowledgeOwnerReadModel ToReadModel(IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions, KnowledgeOwnerCorpusOption? selectedCorpus, IReadOnlyList<KnowledgeSource> sources) => new(corpusOptions, selectedCorpus, sources.Select(ToSourceOption).ToArray());

    public IReadOnlyList<KnowledgeOwnerCorpusOption> ToCorpusOptions(IReadOnlyList<KnowledgeCorpus> corpora) => corpora.Select(ToCorpusOption).ToArray();

    public KnowledgeOwnerCorpusOption? ResolveSelectedCorpus(IReadOnlyList<KnowledgeOwnerCorpusOption> corpusOptions, Guid? selectedCorpusId) => corpusOptions.FirstOrDefault(option => selectedCorpusId is not null && option.CorpusId == selectedCorpusId.Value) ?? corpusOptions.FirstOrDefault();

    public KnowledgeOwnerCorpusOption ToCorpusOption(KnowledgeCorpus corpus) => new(string.IsNullOrWhiteSpace(corpus.Description) ? corpus.Name : $"{corpus.Name} — {corpus.Description}", $"{(corpus.IsActive ? "Enabled" : "Disabled")}{(string.IsNullOrWhiteSpace(corpus.Description) ? string.Empty : $" · {corpus.Description}")}") { CorpusId = corpus.Id };

    private static KnowledgeOwnerSourceOption ToSourceOption(KnowledgeSource source) => new($"{source.Name} · {source.SourceType} · imported {source.ImportedAt.LocalDateTime:g}") { SourceId = source.Id };
}
