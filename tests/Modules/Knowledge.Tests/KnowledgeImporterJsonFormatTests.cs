using System.Text;
using FluentAssertions;
using MediatR;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Commands;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Importing;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeImporterJsonFormatTests
{
    [Fact]
    public async Task ImportAsync_ImportsTechQaJsonDocumentsAndQuestions()
    {
        InMemoryKnowledgeCorpusRepository corpusRepository = new();
        InMemoryKnowledgeImportRepository importRepository = new();
        KnowledgeImporter importer = new(new OneChunkPerDocumentChunker(), corpusRepository, importRepository);
        string json = """
            [
              {
                "question": "How do I reset the appliance?",
                "answer": "Hold reset for ten seconds.",
                "document": "Reset the appliance by holding reset for ten seconds.",
                "title": "Reset guide"
              }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("techqa-json", json), CancellationToken.None);

        _ = result.Documents.Should().ContainSingle(document => document.Title == "Reset guide" && document.Text.Contains("holding reset", StringComparison.Ordinal));
        _ = result.Questions.Should().ContainSingle(question => question.Question == "How do I reset the appliance?" && question.ExpectedAnswer == "Hold reset for ten seconds.");
        _ = result.Questions[0].ExpectedChunkIds.Should().ContainSingle().And.Contain(result.Chunks[0].Id);
        _ = importRepository.SavedImport.Should().BeSameAs(result);
    }

    [Fact]
    public async Task ImportAsync_ImportsRagEvalJsonContextsAndGroundTruth()
    {
        InMemoryKnowledgeCorpusRepository corpusRepository = new();
        InMemoryKnowledgeImportRepository importRepository = new();
        KnowledgeImporter importer = new(new OneChunkPerDocumentChunker(), corpusRepository, importRepository);
        string json = """
            {
              "data": [
                {
                  "query": "Which plan includes exports?",
                  "ground_truth": "The Pro plan includes exports.",
                  "contexts": ["Starter includes dashboards.", "The Pro plan includes exports."]
                }
              ]
            }
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("rag-eval-json", json), CancellationToken.None);

        _ = result.Documents.Should().ContainSingle(document => document.Text.Contains("Starter includes dashboards", StringComparison.Ordinal) && document.Text.Contains("The Pro plan includes exports", StringComparison.Ordinal));
        _ = result.Questions.Should().ContainSingle(question => question.Question == "Which plan includes exports?" && question.ExpectedAnswer == "The Pro plan includes exports.");
        _ = result.Questions[0].ExpectedChunkIds.Should().ContainSingle().And.Contain(result.Chunks[0].Id);
    }

    private static KnowledgeImportRequest Request(string format, string json) => new()
    {
        CorpusId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        TenantId = "tenant-1",
        Format = format,
        SourceName = "source.json",
        Content = new MemoryStream(Encoding.UTF8.GetBytes(json))
    };

    private sealed class OneChunkPerDocumentChunker : IKnowledgeChunker
    {
        public IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document) => [new() { Text = document.Text, Order = 0, Title = document.Title }];
    }

    private sealed class InMemoryKnowledgeCorpusRepository : IKnowledgeCorpusRepository
    {
        public Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult<KnowledgeCorpus?>(new KnowledgeCorpus { Id = corpusId, Name = "Corpus", TenantId = tenantId, IsActive = true });
        public Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeCorpus>> ListAllActiveCorporaForIndexingAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class InMemoryKnowledgeImportRepository : ISender
    {
        public KnowledgeImportResult? SavedImport { get; private set; }
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is SaveKnowledgeImportCommand saveCommand)
            {
                SavedImport = saveCommand.Result;
                return Task.CompletedTask;
            }

            throw new NotSupportedException();
        }
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
