using FluentAssertions;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Enums;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Evaluation;
using Xunit;

namespace Vitarish.Knowledge.Tests;

public sealed class KnowledgeEvaluatorTests
{
    private static readonly Guid CorpusId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string TenantId = "tenant-1";

    [Fact]
    public async Task EvaluateAsync_DistinguishesExpectedAnswerFromMissingExpectedContext()
    {
        Guid expectedChunkId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        KnowledgeQuestion question = Question(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "What is covered?", "Coverage answer.", [expectedChunkId]);
        KnowledgeEvaluator evaluator = Evaluator([question], []);

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(evaluateAnswers: false), CancellationToken.None);

        KnowledgeEvaluationItem item = run.Items.Should().ContainSingle().Subject;
        _ = item.ShouldHaveAnswered.Should().BeTrue();
        _ = item.HasExpectedContext.Should().BeTrue();
        _ = item.ExpectedContextFoundInTop5.Should().BeFalse();
        _ = item.ExpectedContextFoundInTop10.Should().BeFalse();
        _ = item.ExpectedContextWasRetrieved.Should().BeFalse();
        _ = item.WasAnswered.Should().BeFalse();
        _ = item.GeneratedAnswerMatchedExpectedAnswer.Should().BeNull();
    }

    [Theory]
    [InlineData(5, true, true, true)]
    [InlineData(7, false, true, false)]
    public async Task EvaluateAsync_ReportsExpectedContextRankBucketsAndConfiguredRetrievalSupport(int matchingRank, bool foundInTop5, bool foundInTop10, bool expectedContextWasRetrieved)
    {
        Guid expectedChunkId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        KnowledgeQuestion question = Question(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Where is evidence?", "Evidence answer.", [expectedChunkId]);
        KnowledgeEvaluator evaluator = Evaluator([question], HitsWithExpectedChunkAtRank(expectedChunkId, matchingRank));

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(topK: 5, evaluateAnswers: false), CancellationToken.None);

        KnowledgeEvaluationItem item = run.Items.Should().ContainSingle().Subject;
        _ = item.ExpectedContextFoundInTop5.Should().Be(foundInTop5);
        _ = item.ExpectedContextFoundInTop10.Should().Be(foundInTop10);
        _ = item.ExpectedContextWasRetrieved.Should().Be(expectedContextWasRetrieved);
    }

    [Fact]
    public async Task EvaluateAsync_DoesNotTreatAnsweredResponseAsRetrievalSupportOrCorrectness()
    {
        Guid expectedChunkId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        KnowledgeQuestion question = Question(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Can the answerer guess?", "Ground truth.", [expectedChunkId]);
        KnowledgeEvaluator evaluator = Evaluator([question], [], isAnswered: true, generatedAnswer: "A generated answer.");

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(evaluateAnswers: true), CancellationToken.None);

        KnowledgeEvaluationItem item = run.Items.Should().ContainSingle().Subject;
        _ = item.WasAnswered.Should().BeTrue();
        _ = item.ExpectedContextWasRetrieved.Should().BeFalse();
        _ = item.GeneratedAnswerMatchedExpectedAnswer.Should().BeNull();
        _ = item.GeneratedAnswer.Should().Be("A generated answer.");
    }

    [Fact]
    public async Task EvaluateAsync_DeduplicatesSelectedQuestionsByIdInDeterministicOrder()
    {
        Guid duplicateId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        KnowledgeQuestion firstDuplicate = Question(duplicateId, "First duplicate?", "First answer.", []);
        KnowledgeQuestion unique = Question(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Unique?", "Unique answer.", []);
        KnowledgeQuestion secondDuplicate = Question(duplicateId, "Second duplicate?", "Second answer.", []);
        RecordingRetriever retriever = new([]);
        KnowledgeEvaluator evaluator = Evaluator([firstDuplicate, unique, secondDuplicate], retriever: retriever);

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(evaluateAnswers: false), CancellationToken.None);

        _ = run.Items.Select(item => item.QuestionId).Should().Equal(duplicateId, unique.Id);
        _ = retriever.Questions.Should().Equal("First duplicate?", "Unique?");
    }

    [Fact]
    public async Task EvaluateAsync_TreatsAnyRowLocalGoldChunkAsExpectedContextHit()
    {
        Guid missedExpectedChunkId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        Guid foundExpectedChunkId = Guid.Parse("34343434-3434-3434-3434-343434343434");
        KnowledgeQuestion question = Question(
            Guid.Parse("56565656-5656-5656-5656-565656565656"),
            "Which gold chunk is enough?",
            "Any row-local gold evidence is enough.",
            [missedExpectedChunkId, foundExpectedChunkId]);
        KnowledgeEvaluator evaluator = Evaluator([question], [Hit(foundExpectedChunkId)]);

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(evaluateAnswers: false), CancellationToken.None);

        KnowledgeEvaluationItem item = run.Items.Should().ContainSingle().Subject;
        _ = item.ExpectedContextFoundInTop5.Should().BeTrue();
        _ = item.ExpectedContextFoundInTop10.Should().BeTrue();
        _ = item.ExpectedContextWasRetrieved.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_RetrievesAtLeastTop10WhenReportingTop10Metric()
    {
        Guid expectedChunkId = Guid.Parse("78787878-7878-7878-7878-787878787878");
        KnowledgeQuestion question = Question(Guid.Parse("90909090-9090-9090-9090-909090909090"), "Where is rank seven?", "Rank seven.", [expectedChunkId]);
        RecordingRetriever retriever = new(HitsWithExpectedChunkAtRank(expectedChunkId, 7));
        KnowledgeEvaluator evaluator = Evaluator([question], retriever);

        KnowledgeEvaluationRun run = await evaluator.EvaluateAsync(Request(topK: 5, evaluateAnswers: false), CancellationToken.None);

        _ = retriever.TopKs.Should().Equal(10);
        KnowledgeEvaluationItem item = run.Items.Should().ContainSingle().Subject;
        _ = item.ExpectedContextFoundInTop5.Should().BeFalse();
        _ = item.ExpectedContextFoundInTop10.Should().BeTrue();
        _ = item.ExpectedContextWasRetrieved.Should().BeFalse();
    }

    private static KnowledgeEvaluationRequest Request(int topK = 5, bool evaluateAnswers = false) => new()
    {
        CorpusId = CorpusId,
        TenantId = TenantId,
        TopK = topK,
        EvaluateAnswers = evaluateAnswers,
        Scope = KnowledgeEvaluationScope.Full
    };

    private static KnowledgeQuestion Question(Guid id, string text, string? expectedAnswer, IReadOnlyList<Guid> expectedChunkIds) => new()
    {
        Id = id,
        CorpusId = CorpusId,
        Question = text,
        ExpectedAnswer = expectedAnswer,
        ExpectedChunkIds = expectedChunkIds
    };

    private static KnowledgeEvaluator Evaluator(IReadOnlyList<KnowledgeQuestion> questions, IReadOnlyList<KnowledgeHit> hits, bool isAnswered = false, string generatedAnswer = "") =>
        Evaluator(questions, new RecordingRetriever(hits), isAnswered, generatedAnswer);

    private static KnowledgeEvaluator Evaluator(IReadOnlyList<KnowledgeQuestion> questions, RecordingRetriever retriever, bool isAnswered = false, string generatedAnswer = "") =>
        new(new InMemoryQuestionRepository(questions), retriever, new FixedAnswerer(isAnswered, generatedAnswer));

    private static IReadOnlyList<KnowledgeHit> HitsWithExpectedChunkAtRank(Guid expectedChunkId, int rank)
    {
        return [.. Enumerable.Range(1, rank).Select(index => Hit(index == rank ? expectedChunkId : Guid.NewGuid()))];
    }

    private static KnowledgeHit Hit(Guid chunkId) => new()
    {
        ChunkId = chunkId,
        SourceId = Guid.NewGuid(),
        DocumentId = Guid.NewGuid(),
        Text = "Retrieved text."
    };

    private sealed class InMemoryQuestionRepository(IReadOnlyList<KnowledgeQuestion> questions) : IKnowledgeQuestionRepository
    {
        public Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken = default) => Task.FromResult(questions);
    }

    private sealed class RecordingRetriever(IReadOnlyList<KnowledgeHit> hits) : IKnowledgeRetriever
    {
        public List<string> Questions { get; } = [];
        public List<int> TopKs { get; } = [];

        public Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken)
        {
            Questions.Add(query.Question);
            TopKs.Add(query.TopK);
            return Task.FromResult(hits);
        }
    }

    private sealed class FixedAnswerer(bool isAnswered, string generatedAnswer) : IKnowledgeAnswerer
    {
        public Task<KnowledgeAnswer> AskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<KnowledgeAnswer> AnswerFromHitsAsync(KnowledgeAskRequest request, IReadOnlyList<KnowledgeHit> hits, CancellationToken cancellationToken) => Task.FromResult(new KnowledgeAnswer
        {
            Answer = generatedAnswer,
            IsAnswered = isAnswered,
            Citations = [],
            RetrievedContext = hits
        });
    }
}
