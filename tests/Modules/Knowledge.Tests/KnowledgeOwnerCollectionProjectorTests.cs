using FluentAssertions;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Extensions;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeOwnerCollectionProjectorTests
{
    [Fact]
    public void ToOwnerCollection_SelectingCorpusWithMultipleSources_DoesNotDefaultSourceFocus()
    {
        Guid corpusId = Guid.NewGuid();
        KnowledgeOwnerCollectionReadModel model = KnowledgeOwnerCollectionProjector.ToOwnerCollection(
            [Corpus(corpusId, "Clinical policies")],
            SourcesByCorpus(corpusId, Source(Guid.NewGuid(), corpusId, "Imported handbook"), Source(Guid.NewGuid(), corpusId, "Imported FAQ")),
            EmptyFiles(),
            corpusId,
            selectedSourceId: null);

        _ = model.SelectedCorpusId.Should().Be(corpusId);
        _ = model.FocusedSourceId.Should().BeNull();
        _ = model.Corpora.Single().Sources.Should().HaveCount(2);
    }

    [Fact]
    public void ToOwnerCollection_PreservesExplicitSourceFocus_WhenSourceBelongsToSelectedCorpus()
    {
        Guid selectedCorpusId = Guid.NewGuid();
        Guid focusedSourceId = Guid.NewGuid();

        KnowledgeOwnerCollectionReadModel model = KnowledgeOwnerCollectionProjector.ToOwnerCollection(
            [Corpus(selectedCorpusId, "Selected"), Corpus(Guid.NewGuid(), "Other")],
            SourcesByCorpus(selectedCorpusId, Source(Guid.NewGuid(), selectedCorpusId, "Other selected source"), Source(focusedSourceId, selectedCorpusId, "Focused source")),
            EmptyFiles(),
            selectedCorpusId,
            focusedSourceId);

        _ = model.SelectedCorpusId.Should().Be(selectedCorpusId);
        _ = model.FocusedSourceId.Should().Be(focusedSourceId);
    }

    [Fact]
    public void ToOwnerCollection_ClearsExplicitSourceFocus_WhenSourceDoesNotBelongToSelectedCorpus()
    {
        Guid selectedCorpusId = Guid.NewGuid();
        Guid otherCorpusId = Guid.NewGuid();
        Guid otherCorpusSourceId = Guid.NewGuid();

        KnowledgeOwnerCollectionReadModel model = KnowledgeOwnerCollectionProjector.ToOwnerCollection(
            [Corpus(selectedCorpusId, "Selected"), Corpus(otherCorpusId, "Other")],
            new Dictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>>
            {
                [selectedCorpusId] = [Source(Guid.NewGuid(), selectedCorpusId, "Selected source")],
                [otherCorpusId] = [Source(otherCorpusSourceId, otherCorpusId, "Other source")]
            },
            EmptyFiles(),
            selectedCorpusId,
            otherCorpusSourceId);

        _ = model.SelectedCorpusId.Should().Be(selectedCorpusId);
        _ = model.FocusedSourceId.Should().BeNull();
    }

    [Fact]
    public void KnowledgeAskRequest_RemainsCorpusScopedWithoutSourceFilter()
    {
        string[] propertyNames = typeof(KnowledgeAskRequest).GetProperties().Select(property => property.Name).ToArray();

        _ = propertyNames.Should().Contain(nameof(KnowledgeAskRequest.CorpusId));
        _ = propertyNames.Should().NotContain("SourceId");
    }

    private static KnowledgeCorpusEntity Corpus(Guid id, string name) => new()
    {
        Id = id,
        TenantId = "tenant",
        Name = name,
        IsActive = true
    };

    private static KnowledgeSourceEntity Source(Guid id, Guid corpusId, string name) => new()
    {
        Id = id,
        TenantId = "tenant",
        CorpusId = corpusId,
        Name = name,
        SourceType = "file",
        ImportedAt = DateTimeOffset.UtcNow
    };

    private static IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>> SourcesByCorpus(Guid corpusId, params KnowledgeSourceEntity[] sources)
        => new Dictionary<Guid, IReadOnlyList<KnowledgeSourceEntity>> { [corpusId] = sources };

    private static IReadOnlyDictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>> EmptyFiles() => new Dictionary<Guid, IReadOnlyList<KnowledgeOwnerSourceFileProjection>>();
}
