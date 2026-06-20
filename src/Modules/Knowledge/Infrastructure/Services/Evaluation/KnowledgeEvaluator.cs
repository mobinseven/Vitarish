using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Enums;
using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Evaluation;

public sealed class KnowledgeEvaluator(
    IKnowledgeQuestionRepository repository,
    IKnowledgeRetriever retriever,
    IKnowledgeAnswerer answerer) : IKnowledgeEvaluator
{
    private const int RecallAt5 = 5;
    private const int RecallAt10 = 10;

    public async Task<KnowledgeEvaluationRun> EvaluateAsync(
        KnowledgeEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        IReadOnlyList<KnowledgeQuestion> allQuestions =
            await repository.GetQuestionsAsync(
                request.CorpusId,
                request.TenantId,
                cancellationToken);

        IReadOnlyList<KnowledgeQuestion> questions = SelectQuestions(allQuestions, request);

        List<KnowledgeEvaluationItem> items = new(capacity: questions.Count);

        // Contains ranks for all questions that have expected context.
        // Missing context is stored as 0, not ignored.
        List<int> expectedContextRanks = [];

        int retrievalTopK = Math.Max(request.TopK, RecallAt10);

        foreach (KnowledgeQuestion question in questions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(
                new KnowledgeQuery
                {
                    CorpusId = request.CorpusId,
                    Question = question.Question,
                    TenantId = request.TenantId,
                    TopK = retrievalTopK
                },
                cancellationToken);

            bool hasExpectedContext = question.ExpectedChunkIds.Count > 0;

            int expectedContextRank = hasExpectedContext
                ? FirstExpectedRank(question.ExpectedChunkIds, hits)
                : 0;

            if (hasExpectedContext)
            {
                expectedContextRanks.Add(expectedContextRank);
            }

            KnowledgeAnswer? answer = null;

            if (request.EvaluateAnswers)
            {
                answer = await answerer.AnswerFromHitsAsync(
                    new KnowledgeAskRequest
                    {
                        CorpusId = request.CorpusId,
                        Question = question.Question,
                        TenantId = request.TenantId,
                        TopK = request.TopK,
                        SaveTrace = false,
                        TraceKind = "evaluation"
                    },
                    hits,
                    cancellationToken);
            }

            items.Add(new KnowledgeEvaluationItem
            {
                QuestionId = question.Id,
                HasExpectedContext = hasExpectedContext,

                ExpectedContextFoundInTop5 =
                    expectedContextRank is > 0 and <= RecallAt5,

                ExpectedContextFoundInTop10 =
                    expectedContextRank is > 0 and <= RecallAt10,

                WasAnswered = answer?.IsAnswered ?? false,

                ShouldHaveAnswered =
                    !string.IsNullOrWhiteSpace(question.ExpectedAnswer),

                GeneratedAnswer = answer?.Answer
            });
        }

        IReadOnlyList<KnowledgeEvaluationItem> retrievableItems =
            [.. items.Where(static item => item.HasExpectedContext)];

        double recallAt5 = Ratio(
            retrievableItems,
            static item => item.ExpectedContextFoundInTop5);

        double recallAt10 = Ratio(
            retrievableItems,
            static item => item.ExpectedContextFoundInTop10);

        double meanRank = MeanRankIncludingMisses(expectedContextRanks);

        return new KnowledgeEvaluationRun(
            Guid.NewGuid(),
            request.CorpusId,
            request.TenantId,
            items,
            recallAt5,
            recallAt10,
            meanRank);
    }

    private static void ValidateRequest(KnowledgeEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);

        if (request.TopK <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TopK),
                request.TopK,
                "TopK must be greater than zero.");
        }

        if (request.SampleSize is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.SampleSize),
                request.SampleSize,
                "SampleSize must be greater than zero when provided.");
        }
    }

    private static IReadOnlyList<KnowledgeQuestion> SelectQuestions(
        IReadOnlyList<KnowledgeQuestion> questions,
        KnowledgeEvaluationRequest request)
    {
        if (questions.Count == 0)
        {
            return questions;
        }

        if (request.Scope == KnowledgeEvaluationScope.Full)
        {
            return questions;
        }

        int sampleSize = request.SampleSize;

        if (sampleSize >= questions.Count)
        {
            return questions;
        }

        Random random = request.RandomSeed.HasValue
            ? new Random(request.RandomSeed.Value)
            : Random.Shared;

        return TakeRandomSample(questions, sampleSize, random);
    }

    private static IReadOnlyList<KnowledgeQuestion> TakeRandomSample(
        IReadOnlyList<KnowledgeQuestion> questions,
        int sampleSize,
        Random random)
    {
        int[] indexes = Enumerable.Range(0, questions.Count).ToArray();

        // Partial Fisher-Yates shuffle.
        // This avoids sorting the whole collection by random values.
        for (int i = 0; i < sampleSize; i++)
        {
            int swapIndex = random.Next(i, indexes.Length);
            (indexes[i], indexes[swapIndex]) = (indexes[swapIndex], indexes[i]);
        }

        KnowledgeQuestion[] sample = new KnowledgeQuestion[sampleSize];

        for (int i = 0; i < sampleSize; i++)
        {
            sample[i] = questions[indexes[i]];
        }

        return sample;
    }

    private static int FirstExpectedRank(
        HashSet<Guid> expectedChunkIds,
        IReadOnlyList<KnowledgeHit> hits)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            if (expectedChunkIds.Contains(hits[i].ChunkId))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private static int FirstExpectedRank(
        IReadOnlyList<Guid> expectedChunkIds,
        IReadOnlyList<KnowledgeHit> hits)
    {
        if (expectedChunkIds.Count == 0 || hits.Count == 0)
        {
            return 0;
        }

        HashSet<Guid> expectedChunkIdSet = [.. expectedChunkIds];

        return FirstExpectedRank(expectedChunkIdSet, hits);
    }

    private static double Ratio(
        IReadOnlyList<KnowledgeEvaluationItem> items,
        Func<KnowledgeEvaluationItem, bool> predicate) => items.Count == 0
            ? 0
            : items.Count(predicate) / (double)items.Count;

    private static double MeanRankIncludingMisses(IReadOnlyList<int> ranks)
    {
        if (ranks.Count == 0)
        {
            return 0;
        }

        // Misses are rank 0.
        // For average rank, treating misses as 0 would incorrectly improve the score.
        // Therefore we penalize misses as one position after the worst possible Top10 target.
        const int missedRankPenalty = RecallAt10 + 1;

        return ranks.Average(rank => rank > 0 ? rank : missedRankPenalty);
    }
}
