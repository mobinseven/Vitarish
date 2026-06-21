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
    public async Task ImportAsync_MapsTechQaQuestionToChunksFromSameJsonRow()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              {
                "question": "Why does alpha fail?",
                "answer": "Alpha fails because task A returns 404.",
                "document": "Alpha task A returns 404.",
                "title": "Alpha"
              },
              {
                "question": "Why does beta fail?",
                "answer": "Beta fails because task B times out.",
                "document": "Beta task B times out.",
                "title": "Beta"
              }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("techqa-json", json), CancellationToken.None);

        KnowledgeQuestion alphaQuestion = result.Questions.Single(question => question.Question == "Why does alpha fail?");
        KnowledgeQuestion betaQuestion = result.Questions.Single(question => question.Question == "Why does beta fail?");
        KnowledgeChunk alphaChunk = result.Chunks.Single(chunk => chunk.Text.Contains("Alpha task", StringComparison.Ordinal));
        KnowledgeChunk betaChunk = result.Chunks.Single(chunk => chunk.Text.Contains("Beta task", StringComparison.Ordinal));

        _ = alphaQuestion.ExpectedChunkIds.Should().Equal(alphaChunk.Id);
        _ = betaQuestion.ExpectedChunkIds.Should().Equal(betaChunk.Id);
    }

    [Fact]
    public async Task ImportAsync_UsesRowLocalDocumentWhenAnswerIsParaphrased()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              {
                "question": "Why does the upgrade fail?",
                "answer": "Because remote task execution returns 404.",
                "document": "The import-nodes task failed with TASK001X. Remote task execution returned response code 404."
              }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("techqa-json", json), CancellationToken.None);

        _ = result.Questions.Should().ContainSingle().Subject.ExpectedChunkIds.Should().Equal(result.Chunks.Single().Id);
    }

    [Fact]
    public async Task ImportAsync_ReadsContextArraysAsRowLocalGoldEvidence()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              {
                "question": "Why does the upgrade fail?",
                "answer": "Because remote task execution returns 404.",
                "documents": [
                  { "title": "Portal upgrade technote", "text": "The import-nodes task failed with TASK001X." },
                  { "title": "Cause", "content": "Remote task execution returned response code 404." }
                ]
              }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("techqa-json", json), CancellationToken.None);

        _ = result.Documents.Should().ContainSingle().Subject.Text.Should().Contain("The import-nodes task failed").And.Contain("Remote task execution returned response code 404");
        _ = result.Questions.Should().ContainSingle().Subject.ExpectedChunkIds.Should().Equal(result.Chunks.Single().Id);
    }

    [Fact]
    public async Task ImportAsync_FallsBackToExpectedContextTextsWhenNoRowLocalDocumentExists()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              {
                "question": "What does the shared evidence say?",
                "answer": "Shared evidence answers the fallback question.",
                "expected_context": "Shared evidence answers the fallback question."
              },
              {
                "question": "What creates the shared evidence?",
                "answer": "The second row creates it.",
                "document": "Shared evidence answers the fallback question."
              }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("techqa-json", json), CancellationToken.None);

        KnowledgeQuestion fallbackQuestion = result.Questions.Single(question => question.Question == "What does the shared evidence say?");
        _ = fallbackQuestion.ExpectedChunkIds.Should().Equal(result.Chunks.Single().Id);
    }

    [Fact]
    public async Task ImportAsync_ImportsTechQaJsonDocumentsAndQuestions()
    {
        KnowledgeImporter importer = Importer(out InMemoryKnowledgeImportRepository importRepository);
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
        KnowledgeImporter importer = Importer(out _);
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

    [Fact]
    public async Task ImportAsync_ImportsPlainQaJsonArrayAsRowLocalAnswerDocuments()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              { "id": "alpha", "question": "What fails for alpha?", "answer": "Alpha evidence only." },
              { "id": "beta", "question": "What fails for beta?", "answer": "Beta evidence only." }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("plain-qa-json", json), CancellationToken.None);

        _ = result.Questions.Should().HaveCount(2);
        _ = result.Documents.Should().HaveCount(2);
        _ = result.Chunks.Should().HaveCount(2);
        _ = result.Documents.Should().Contain(document => document.Title == "source.json item alpha" && document.Text == "Alpha evidence only.");
        _ = result.Documents.Should().Contain(document => document.Title == "source.json item beta" && document.Text == "Beta evidence only.");
    }

    [Fact]
    public async Task ImportAsync_MapsPlainQaJsonQuestionsOnlyToOwnAnswerChunks()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              { "id": "alpha", "question": "What answer belongs to alpha?", "answer": "The alpha-only answer." },
              { "id": "beta", "question": "What answer belongs to beta?", "answer": "The beta-only answer." }
            ]
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("plain-qa-json", json), CancellationToken.None);

        KnowledgeQuestion alphaQuestion = result.Questions.Single(question => question.Question == "What answer belongs to alpha?");
        KnowledgeQuestion betaQuestion = result.Questions.Single(question => question.Question == "What answer belongs to beta?");
        KnowledgeChunk alphaChunk = result.Chunks.Single(chunk => chunk.Text == "The alpha-only answer.");
        KnowledgeChunk betaChunk = result.Chunks.Single(chunk => chunk.Text == "The beta-only answer.");

        _ = alphaQuestion.ExpectedChunkIds.Should().Equal(alphaChunk.Id);
        _ = betaQuestion.ExpectedChunkIds.Should().Equal(betaChunk.Id);
    }

    [Fact]
    public async Task ImportAsync_ReadsPlainQaJsonDataWrapper()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            {
              "data": [
                { "id": "wrapped", "query": "Which wrapper works?", "output": "The data wrapper works." }
              ]
            }
            """;

        KnowledgeImportResult result = await importer.ImportAsync(Request("plain-qa-json", json), CancellationToken.None);

        _ = result.Questions.Should().ContainSingle(question => question.Question == "Which wrapper works?" && question.ExpectedAnswer == "The data wrapper works.");
        _ = result.Documents.Should().ContainSingle(document => document.Title == "source.json item wrapped" && document.Text == "The data wrapper works.");
    }

    [Fact]
    public async Task ImportAsync_RejectsPlainQaJsonWithoutQuestion()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              { "id": "missing-question", "question": " ", "answer": "Answer exists." }
            ]
            """;

        Func<Task> act = () => importer.ImportAsync(Request("plain-qa-json", json), CancellationToken.None);

        _ = await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*plain-qa-json*item 1 requires a question, query, input, or prompt value*");
    }

    [Fact]
    public async Task ImportAsync_RejectsPlainQaJsonWithoutAnswer()
    {
        KnowledgeImporter importer = Importer(out _);
        string json = """
            [
              { "id": "missing-answer", "question": "Question exists?", "answer": " " }
            ]
            """;

        Func<Task> act = () => importer.ImportAsync(Request("plain-qa-json", json), CancellationToken.None);

        _ = await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*plain-qa-json*item 1 requires an answer, expected_answer, ground_truth, reference_answer, or output value*");
    }

    private static KnowledgeImportRequest Request(string format, string json) => new()
    {
        CorpusId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        TenantId = "tenant-1",
        Format = format,
        SourceName = "source.json",
        Content = new MemoryStream(Encoding.UTF8.GetBytes(json))
    };

    private static KnowledgeImporter Importer(out InMemoryKnowledgeImportRepository importRepository)
    {
        InMemoryKnowledgeCorpusRepository corpusRepository = new();
        importRepository = new InMemoryKnowledgeImportRepository();
        return new KnowledgeImporter(new OneChunkPerDocumentChunker(), corpusRepository, importRepository);
    }

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
