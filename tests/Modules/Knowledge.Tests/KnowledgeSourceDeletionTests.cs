#pragma warning disable BL0005

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Handlers;
using Vitarish.Knowledge.Infrastructure.Services;
using Vitarish.Knowledge.Interface.Components;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeSourceDeletionTests
{
    [Fact]
    public async Task DeleteSource_WithExistingCorpusAndSource_DeletesSourceAndReturnsSuccess()
    {
        Guid corpusId = Guid.NewGuid();
        Guid sourceId = Guid.NewGuid();
        var service = new RecordingKnowledgeService { Corpus = Corpus(corpusId), SourceExists = true };
        var handler = new DeleteKnowledgeSourceCommandHandler(service, NullLogger<DeleteKnowledgeSourceCommandHandler>.Instance);

        KnowledgeOwnerCommandResult result = await handler.Handle(Command(corpusId, sourceId), CancellationToken.None);

        _ = result.Succeeded.Should().BeTrue();
        _ = result.Message.Should().Be("Source deleted.");
        _ = service.DeleteCalls.Should().Be(1);
    }

    [Fact]
    public async Task DeleteSource_WithMissingCorpus_ReturnsCorpusNotFoundAndDoesNotMutateState()
    {
        var service = new RecordingKnowledgeService { Corpus = null, SourceExists = true };
        var handler = new DeleteKnowledgeSourceCommandHandler(service, NullLogger<DeleteKnowledgeSourceCommandHandler>.Instance);

        KnowledgeOwnerCommandResult result = await handler.Handle(Command(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        _ = result.Succeeded.Should().BeFalse();
        _ = result.Message.Should().Be("The selected corpus was not found.");
        _ = service.SourceExistsCalls.Should().Be(0);
        _ = service.DeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSource_WithMissingSource_ReturnsSourceNotFoundAndDoesNotDelete()
    {
        Guid corpusId = Guid.NewGuid();
        var service = new RecordingKnowledgeService { Corpus = Corpus(corpusId), SourceExists = false };
        var handler = new DeleteKnowledgeSourceCommandHandler(service, NullLogger<DeleteKnowledgeSourceCommandHandler>.Instance);

        KnowledgeOwnerCommandResult result = await handler.Handle(Command(corpusId, Guid.NewGuid()), CancellationToken.None);

        _ = result.Succeeded.Should().BeFalse();
        _ = result.Message.Should().Be("The selected source was not found.");
        _ = service.SourceExistsCalls.Should().Be(1);
        _ = service.DeleteCalls.Should().Be(0);
    }

    [Fact]
    public async Task DeleteSourceAsync_WhenIndexCleanupFails_PropagatesFailureAfterCanonicalDelete()
    {
        Guid corpusId = Guid.NewGuid();
        Guid sourceId = Guid.NewGuid();
        var sourceRepository = new RecordingKnowledgeSourceRepository { DeleteResult = true };
        var indexer = new ThrowingDeleteIndexer();
        var service = new KnowledgeService(new EmptyImporter(), indexer, new EmptyRetriever(), new EmptyAnswerer(), new EmptyEvaluator(), new EmptyCorpusRepository(), sourceRepository);

        Func<Task> act = () => service.DeleteSourceAsync(sourceId, corpusId, "tenant", CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("index unavailable");
        _ = sourceRepository.DeleteCalls.Should().Be(1);
        _ = indexer.DeleteCalls.Should().Be(1);
    }

    [Fact]
    public async Task SourceSelector_WhenSourcesRefreshWithoutSelectedSource_ClearsStaleSelection()
    {
        Guid sourceId = Guid.NewGuid();
        KnowledgeOwnerSourceOption? selected = null;
        var selector = new KnowledgeSourceSelector
        {
            Label = "Source",
            Sources = [new KnowledgeOwnerSourceOption("source") { SourceId = sourceId }],
            SourcesVersion = 1,
            SelectedSourceChanged = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<KnowledgeOwnerSourceOption?>(this, value => selected = value)
        };

        await InvokeParametersSetAsync(selector);
        await (Task)typeof(KnowledgeSourceSelector).GetMethod("SelectSourceAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(selector, [new KnowledgeOwnerSourceOption("source") { SourceId = sourceId }])!;
        _ = selected?.SourceId.Should().Be(sourceId);

        selector.Sources = [];
        selector.SourcesVersion = 2;
        await InvokeParametersSetAsync(selector);

        _ = selected.Should().BeNull();
    }

    private static Task InvokeParametersSetAsync(KnowledgeSourceSelector selector)
        => (Task)typeof(KnowledgeSourceSelector).GetMethod("OnParametersSetAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(selector, [])!;


    [Fact]
    public async Task SourceDeleter_WhenSourcesRefreshWithoutSelectedSource_ClearsStaleSelectionThatDisablesDeleteAction()
    {
        Guid sourceId = Guid.NewGuid();
        var deleter = new KnowledgeSourceDeleter
        {
            Sources = [new KnowledgeOwnerSourceOption("source") { SourceId = sourceId }],
            DeleteRequested = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<DeleteKnowledgeSourceInput>(this, _ => Task.CompletedTask)
        };

        await (Task)typeof(KnowledgeSourceDeleter).GetMethod("SelectSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(deleter, [new KnowledgeOwnerSourceOption("source") { SourceId = sourceId }])!;
        _ = typeof(KnowledgeSourceDeleter).GetField("source", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(deleter).Should().NotBeNull();

        deleter.Sources = [];
        typeof(KnowledgeSourceDeleter).GetMethod("OnParametersSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(deleter, []);

        _ = typeof(KnowledgeSourceDeleter).GetField("source", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(deleter).Should().BeNull();
    }

    private static DeleteKnowledgeSourceCommand Command(Guid corpusId, Guid sourceId) => new("tenant", "owner", new DeleteKnowledgeSourceInput(corpusId, sourceId));

    private static KnowledgeCorpus Corpus(Guid id) => new() { Id = id, Name = "corpus", TenantId = "tenant", IsActive = true };

    private sealed class RecordingKnowledgeService : IKnowledgeService
    {
        public KnowledgeCorpus? Corpus { get; init; }
        public bool SourceExists { get; init; }
        public int SourceExistsCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult(Corpus);
        public Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) { SourceExistsCalls++; return Task.FromResult(SourceExists); }
        public Task<KnowledgeDeleteSourceResult> DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) { DeleteCalls++; return Task.FromResult(new KnowledgeDeleteSourceResult(sourceId, corpusId, tenantId, true, true)); }
        public Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeCorpus>>([]);
        public Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeSource>>([]);
        public Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeIndexResult> IndexAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeHit>> SearchAsync(KnowledgeQuery query, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingKnowledgeSourceRepository : IKnowledgeSourceRepository
    {
        public bool DeleteResult { get; init; }
        public int DeleteCalls { get; private set; }
        public Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeSource>>([]);
        public Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult(DeleteResult);
        public Task<bool> DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) { DeleteCalls++; return Task.FromResult(DeleteResult); }
    }

    private sealed class ThrowingDeleteIndexer : IKnowledgeIndexer
    {
        public int DeleteCalls { get; private set; }
        public Task<KnowledgeIndexResult> IndexCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteFromIndexAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken) { DeleteCalls++; throw new InvalidOperationException("index unavailable"); }
    }

    private sealed class EmptyCorpusRepository : IKnowledgeCorpusRepository
    {
        public Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult<KnowledgeCorpus?>(null);
        public Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeCorpus>>([]);
        public Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeCorpus>>([]);
        public Task<IReadOnlyList<KnowledgeCorpus>> ListAllActiveCorporaForIndexingAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<KnowledgeCorpus>>([]);
    }

    private sealed class EmptyImporter : IKnowledgeImporter
    {
        public Task<KnowledgeImportResult> ImportAsync(KnowledgeImportRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeImportResult> ReplaceSourceAsync(Guid sourceId, KnowledgeImportRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class EmptyRetriever : IKnowledgeRetriever { public Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken) => throw new NotSupportedException(); }
    private sealed class EmptyAnswerer : IKnowledgeAnswerer
    {
        public Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
    private sealed class EmptyEvaluator : IKnowledgeEvaluator { public Task<KnowledgeEvaluationRun> EvaluateAsync(KnowledgeEvaluationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException(); }
}
