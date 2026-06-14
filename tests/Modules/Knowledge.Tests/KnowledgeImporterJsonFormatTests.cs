using System.Text;
using FluentAssertions;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Importing;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeImporterJsonFormatTests
{
    [Fact]
    public async Task ImportAsync_ImportsTechQaJsonDocumentsAndQuestions()
    {
        InMemoryKnowledgeRepository repository = new();
        KnowledgeImporter importer = new(new OneChunkPerDocumentChunker(), repository);
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
        _ = repository.SavedImport.Should().BeSameAs(result);
    }

    [Fact]
    public async Task ImportAsync_ImportsRagEvalJsonContextsAndGroundTruth()
    {
        InMemoryKnowledgeRepository repository = new();
        KnowledgeImporter importer = new(new OneChunkPerDocumentChunker(), repository);
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
        CorpusId = "corpus-1",
        TenantId = "tenant-1",
        Format = format,
        SourceName = "source.json",
        Content = new MemoryStream(Encoding.UTF8.GetBytes(json))
    };

    private sealed class OneChunkPerDocumentChunker : IKnowledgeChunker
    {
        public IReadOnlyList<KnowledgeChunkDraft> Chunk(KnowledgeDocument document) => [new() { Text = document.Text, Order = 0, Title = document.Title }];
    }

    private sealed class InMemoryKnowledgeRepository : IKnowledgeRepository
    {
        public KnowledgeImportResult? SavedImport { get; private set; }

        public Task<KnowledgeCorpus?> GetCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken) => Task.FromResult<KnowledgeCorpus?>(new KnowledgeCorpus { Id = corpusId, Name = "Corpus", TenantId = tenantId, IsActive = true });

        public Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken)
        {
            SavedImport = result;
            return Task.CompletedTask;
        }

        public Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(string corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<string> corpusIds, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(string corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(string corpusId, string tenantId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
