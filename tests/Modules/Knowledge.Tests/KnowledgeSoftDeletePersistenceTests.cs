using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Caching;
using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Knowledge.Application.Queries;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Handlers;
using Vitarish.Knowledge.Infrastructure.Repositories;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeSoftDeletePersistenceTests
{
    [Fact]
    public async Task GetSourcesAsync_ExcludesSoftDeletedSourcesAndPreservesTenantScope()
    {
        await using KnowledgeDbContext dbContext = CreateDbContext("tenant-a");
        Guid corpusId = Guid.NewGuid();
        KnowledgeSourceEntity activeSource = Source(corpusId, "tenant-a", "Active source");
        KnowledgeSourceEntity deletedSource = Source(corpusId, "tenant-a", "Deleted source", DateTime.UtcNow);
        dbContext.Sources.AddRange(activeSource, deletedSource);
        _ = await dbContext.SaveChangesAsync();
        var repository = new KnowledgeSourceRepository(dbContext);

        var sources = await repository.GetSourcesAsync(corpusId, "tenant-a", CancellationToken.None);

        _ = sources.Select(source => source.Id).Should().Equal(activeSource.Id);
        var tenantBSourceIds = await repository.GetSourcesAsync(corpusId, "tenant-b", CancellationToken.None);
        _ = tenantBSourceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task OwnerCollectionQuery_ExcludesSoftDeletedSourcesFromProjection()
    {
        await using KnowledgeDbContext dbContext = CreateDbContext("tenant-a");
        Guid corpusId = Guid.NewGuid();
        KnowledgeCorpusEntity corpus = Corpus(corpusId, "tenant-a");
        KnowledgeSourceEntity activeSource = Source(corpusId, "tenant-a", "Active source");
        KnowledgeSourceEntity deletedSource = Source(corpusId, "tenant-a", "Deleted source", DateTime.UtcNow);
        dbContext.Corpora.Add(corpus);
        dbContext.Sources.AddRange(activeSource, deletedSource);
        _ = await dbContext.SaveChangesAsync();
        var handler = new KnowledgeOwnerCollectionQueryHandler(dbContext);

        var model = await handler.Handle(new KnowledgeOwnerCollectionQuery("tenant-a", "owner", corpusId, null), CancellationToken.None);

        _ = model.Corpora.Should().ContainSingle();
        _ = model.Corpora[0].Sources.Select(source => source.SourceId).Should().Equal(activeSource.Id);
    }

    [Fact]
    public async Task DeleteSourceAsync_SoftDeletesSourceDocumentsChunksAndAssociatedQuestions()
    {
        await using KnowledgeDbContext dbContext = CreateDbContext("tenant-a", includeEntityInterceptor: true);
        Guid corpusId = Guid.NewGuid();
        KnowledgeSourceEntity source = Source(corpusId, "tenant-a", "Source");
        KnowledgeDocumentEntity document = Document(corpusId, source.Id, "tenant-a");
        KnowledgeChunkEntity chunk = Chunk(corpusId, source.Id, document.Id, "tenant-a");
        KnowledgeQuestionEntity question = Question(corpusId, chunk.Id);
        dbContext.AddRange(source, document, chunk, question);
        _ = await dbContext.SaveChangesAsync();
        var repository = new KnowledgeSourceRepository(dbContext);

        bool deleted = await repository.DeleteSourceAsync(source.Id, corpusId, "tenant-a", CancellationToken.None);

        _ = deleted.Should().BeTrue();
        DateTime? sourceDeletedAt = await dbContext.Sources.IgnoreQueryFilters().Where(row => row.Id == source.Id).Select(row => row.DeletedAt).SingleAsync();
        DateTime? documentDeletedAt = await dbContext.Documents.IgnoreQueryFilters().Where(row => row.Id == document.Id).Select(row => row.DeletedAt).SingleAsync();
        DateTime? chunkDeletedAt = await dbContext.Chunks.IgnoreQueryFilters().Where(row => row.Id == chunk.Id).Select(row => row.DeletedAt).SingleAsync();
        DateTime? questionDeletedAt = await dbContext.Questions.IgnoreQueryFilters().Where(row => row.Id == question.Id).Select(row => row.DeletedAt).SingleAsync();

        _ = sourceDeletedAt.Should().NotBeNull();
        _ = documentDeletedAt.Should().NotBeNull();
        _ = chunkDeletedAt.Should().NotBeNull();
        _ = questionDeletedAt.Should().NotBeNull();
    }

    private static KnowledgeDbContext CreateDbContext(string tenantId, bool includeEntityInterceptor = false)
    {
        var options = new DbContextOptionsBuilder<KnowledgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        IInterceptor[] interceptors = includeEntityInterceptor ? [new EntityInterceptor(new FixedTimeService(), new FixedUserService(), new NoopCacheInvalidationService(), new NoopPublisher())] : [];
        return new KnowledgeDbContext(options, interceptors, new StubRequestContext(tenantId), new HttpContextAccessor());
    }

    private static KnowledgeCorpusEntity Corpus(Guid id, string tenantId) => new() { Id = id, Name = "Corpus", TenantId = tenantId, IsActive = true };

    private static KnowledgeSourceEntity Source(Guid corpusId, string tenantId, string name, DateTime? deletedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        CorpusId = corpusId,
        TenantId = tenantId,
        Name = name,
        SourceType = "file",
        ImportedAt = DateTimeOffset.UtcNow,
        DeletedAt = deletedAt
    };

    private static KnowledgeDocumentEntity Document(Guid corpusId, Guid sourceId, string tenantId) => new() { Id = Guid.NewGuid(), CorpusId = corpusId, SourceId = sourceId, TenantId = tenantId, Title = "Document", Text = "Text" };

    private static KnowledgeChunkEntity Chunk(Guid corpusId, Guid sourceId, Guid documentId, string tenantId) => new() { Id = Guid.NewGuid(), CorpusId = corpusId, SourceId = sourceId, DocumentId = documentId, TenantId = tenantId, Text = "Chunk", Order = 1 };

    private static KnowledgeQuestionEntity Question(Guid corpusId, Guid chunkId) => new() { Id = Guid.NewGuid(), CorpusId = corpusId, Question = "Question?", ExpectedChunkIds = [chunkId] };

    private sealed class StubRequestContext(string tenantId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner";
        public string? UserId => "user";
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => throw new NotSupportedException();
    }

    private sealed class FixedTimeService : IEntityTimeService
    {
        public DateTime Now { get; private set; } = new(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc);
        public void Set(DateTime? now) => Now = now ?? Now;
    }

    private sealed class FixedUserService : IEntityUserService
    {
        public string? UserId { get; private set; } = "user";
        public void Set(string? userId) => UserId = userId;
    }

    private sealed class NoopCacheInvalidationService : IEntityCacheInvalidationService
    {
        public void Invalidate(Entity entity, EntityState entityState) { }
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
